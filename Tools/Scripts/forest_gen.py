# 시작의 숲 본생성 러너: create_map_object 큐잉 + 폴링 + PNG 즉시 로컬 확보 (서버 8h 삭제)
# usage: forest_gen.py <group>   group = probe | terrain | background | props | all_objects
import base64, json, os, sys, time
from mcp import call_tool, tool_text
import forest_prompts as P

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main"
os.makedirs(OUT, exist_ok=True)
REF = os.path.join(OUT, "ref_style_192.png")


def queue(name, args):
    """create_map_object 큐잉 (레이트 리밋 30s 백오프)."""
    for attempt in range(6):
        r = call_tool("create_map_object", args)
        txt = tool_text(r)
        if "rate limit" in txt.lower():
            print(f"  {name}: rate limited -> wait 30s (attempt {attempt})")
            time.sleep(30)
            continue
        oid = None
        for line in txt.splitlines():
            if line.strip().lower().startswith("id:"):
                oid = line.split(":", 1)[1].strip()
        if oid is None:
            print(f"  {name}: NO ID. RAW: {txt[:400]}")
        return oid
    return None


def poll(ids, timeout=900):
    """{name: id} 를 폴링해 완료분 PNG 저장. 저장된 이름 집합 반환."""
    pending = dict(ids)
    saved = []
    deadline = time.time() + timeout
    while pending and time.time() < deadline:
        for name, oid in list(pending.items()):
            r = call_tool("get_map_object", {"object_id": oid})
            got = False
            for p in r.get("result", {}).get("content", []):
                if p.get("type") == "image":
                    with open(os.path.join(OUT, name + ".png"), "wb") as f:
                        f.write(base64.b64decode(p["data"]))
                    print(f"  SAVED {name}.png")
                    saved.append(name)
                    got = True
                    break
            if got:
                del pending[name]
            else:
                txt = tool_text(r)
                if "failed" in txt.lower():
                    print(f"  FAILED {name}: {txt[:200]}")
                    del pending[name]
            time.sleep(2)
        if pending:
            print("  waiting...", list(pending))
            time.sleep(12)
    if pending:
        print("  TIMEOUT still pending:", pending)
    return saved


def run(jobs):
    ids = {}
    for name, args in jobs:
        oid = queue(name, args)
        ids[name] = oid
        print(f"queued {name}: {oid}")
        time.sleep(15)
    ids = {k: v for k, v in ids.items() if v}
    with open(os.path.join(OUT, "_ids.json"), "a") as f:
        f.write(json.dumps(ids, indent=1) + "\n")
    return poll(ids)


def prop_jobs():
    with open(REF, "rb") as f:
        ref_b64 = base64.b64encode(f.read()).decode()
    bg = json.dumps({"type": "base64", "base64": ref_b64})
    jobs = []
    for name, desc, inp in P.PROPS:
        jobs.append((name, dict(description=desc, background_image=bg,
                                inpainting=json.dumps(inp), **P.COMMON)))
    return jobs


if __name__ == "__main__":
    g = sys.argv[1]
    if g == "probe":
        run([P.TERRAIN[0]])                 # ground_flat_02 1장 = 소모 측정용
    elif g == "terrain":
        run(P.TERRAIN[1:])                  # 나머지 지형 9장
    elif g == "background":
        run(P.BACKGROUND)
    elif g == "props":
        run(prop_jobs())
    elif g == "props1":
        run(prop_jobs()[:1])                # 인페인팅 반환형식 실측용 1장
    elif g == "propsrest":
        run(prop_jobs()[1:])
    else:
        print("unknown group", g)
