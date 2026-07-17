# UI 아이콘 생성기 (루비 / 잠금) — create_map_object 큐잉 + 폴링 + PNG 저장
# usage: ui_icons_gen.py <group>   group = ruby | lock | all
import base64, json, os, sys, time
from mcp import call_tool, tool_text

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\ui_icons"
os.makedirs(OUT, exist_ok=True)

# 스타일 앵커 (지형과 공유하는 세계관 톤). UI 아이콘은 팔레트를 개별 오버라이드한다.
ANCHOR = ("Korean Joseon folk pixel art, dark warm brown single color outline (no pure black), "
          "tranquil ink-wash mood. ")

# 20px에서 읽혀야 하므로: 굵은 실루엣, 디테일 최소, 화면 채우기
ICON = ("Single centered game UI icon on a fully transparent background. Bold thick chunky "
        "silhouette that fills most of the canvas, very simple readable shape, minimal internal "
        "detail so it stays legible when scaled down to 20 pixels. No text, no letters, no frame, "
        "no border, no drop shadow, no ground shadow. ")

RUBY = [
    ("ruby_a_cushion", dict(description=ANCHOR + ICON +
        "A single faceted ruby gemstone. Classic cushion-cut gem viewed straight on: a wide flat "
        "table facet across the top, angled shoulder facets, tapering down to a single point at the "
        "bottom. Bold geometric facets. Vivid crimson red body with one darker red shade for the "
        "lower facets and one bright pink-white sparkle highlight on the upper left facet.",
        width=128, height=128, view="side", outline="single color outline",
        shading="basic shading", detail="low detail")),
    ("ruby_b_round", dict(description=ANCHOR + ICON +
        "A single round polished ruby bead gemstone, viewed straight on. A bold circular red jewel "
        "with a bright pink-white crescent highlight in the upper left and a deep dark red shadow "
        "along the lower right edge. Very simple, only three shades of red.",
        width=128, height=128, view="side", outline="single color outline",
        shading="basic shading", detail="low detail")),
    ("ruby_c_teardrop", dict(description=ANCHOR + ICON +
        "A single faceted ruby gemstone shaped like a bold wide diamond / rhombus, viewed straight "
        "on, wider than it is tall at the shoulders and tapering to a clean point at the bottom. "
        "Flat bold color blocks of vivid crimson red, one dark red, one bright highlight. "
        "Extremely simple and graphic.",
        width=128, height=128, view="side", outline="single color outline",
        shading="flat shading", detail="low detail")),
]

LOCK = [
    ("lock_a_fish", dict(description=ANCHOR + ICON +
        "A traditional Korean Joseon fish-shaped padlock (bungeo jamulsoe) seen from the side. A "
        "horizontal rounded brass fish-shaped lock body with a small simple keyhole slot in the "
        "middle, and a thick U-shaped shackle arching over the top. Aged brass and dark iron.",
        width=128, height=128, view="side", outline="single color outline",
        shading="basic shading", detail="low detail")),
    ("lock_b_fish_simple", dict(description=ANCHOR + ICON +
        "A traditional Korean fish-shaped iron padlock, side view, extremely simplified to a bold "
        "graphic icon: a fat horizontal rounded fish body with a blunt head on the left, a small "
        "triangular tail fin on the right, one keyhole dot in the center, and a thick square "
        "U-shaped shackle loop rising from the top edge. Only two or three flat shades.",
        width=128, height=128, view="side", outline="single color outline",
        shading="flat shading", detail="low detail")),
    ("lock_c_padlock", dict(description=ANCHOR + ICON +
        "A simple sturdy antique Korean iron padlock, front view: a bold rounded rectangular lock "
        "body with a thick U-shaped shackle arching over the top and a single keyhole in the "
        "center of the body. Aged dark iron with soft highlights. Bold and blocky.",
        width=128, height=128, view="side", outline="single color outline",
        shading="basic shading", detail="low detail")),
]


def queue(name, args):
    for attempt in range(6):
        r = call_tool("create_map_object", args)
        txt = tool_text(r)
        if "rate limit" in txt.lower():
            print(f"  {name}: rate limited -> wait 30s (attempt {attempt})", flush=True)
            time.sleep(30)
            continue
        oid = None
        for line in txt.splitlines():
            if line.strip().lower().startswith("id:"):
                oid = line.split(":", 1)[1].strip()
        if oid is None:
            print(f"  {name}: NO ID. RAW: {txt[:300]}", flush=True)
        return oid
    return None


def poll(ids, timeout=900):
    pending = dict(ids)
    deadline = time.time() + timeout
    while pending and time.time() < deadline:
        for name, oid in list(pending.items()):
            r = call_tool("get_map_object", {"object_id": oid})
            got = False
            for p in r.get("result", {}).get("content", []):
                if p.get("type") == "image":
                    with open(os.path.join(OUT, name + ".png"), "wb") as f:
                        f.write(base64.b64decode(p["data"]))
                    print(f"  SAVED {name}.png", flush=True)
                    got = True
                    break
            if got:
                del pending[name]
            else:
                txt = tool_text(r)
                if "failed" in txt.lower():
                    print(f"  FAILED {name}: {txt[:200]}", flush=True)
                    del pending[name]
            time.sleep(2)
        if pending:
            print("  waiting...", list(pending), flush=True)
            time.sleep(12)
    if pending:
        print("  TIMEOUT:", pending, flush=True)


def run(jobs):
    ids = {}
    for name, args in jobs:
        oid = queue(name, args)
        if oid:
            ids[name] = oid
        print(f"queued {name}: {oid}", flush=True)
        time.sleep(8)
    with open(os.path.join(OUT, "_ids.json"), "a") as f:
        f.write(json.dumps(ids, indent=1) + "\n")
    poll(ids)


if __name__ == "__main__":
    g = sys.argv[1] if len(sys.argv) > 1 else "all"
    jobs = {"ruby": RUBY, "lock": LOCK, "all": RUBY + LOCK}[g]
    run(jobs)
