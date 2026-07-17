# 시작의 숲 캐릭터 2벌: 한복(선비, 애니 5) + 현대복(주인공 도착 시, 애니 2)
# 애니는 template/v3 = 1회/방향 (실측 확인된 스펙). directions를 지정해 east 1방향만 소모.
# usage: forest_char.py base | anim | fetch
import json, os, sys, time
from mcp import call_tool, tool_text
import forest_prompts as P

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main"
IDS = os.path.join(OUT, "_char_ids.json")

# PPU 64 기준: 본체 96px = 1.5u (콜라이더 1.3u + 머리 여유 0.2u)
CHARS = [
    ("seonbi_hanbok", P.CHAR_HANBOK, "Seonbi Hanbok Forest"),
    ("player_modern", P.CHAR_MODERN, "Player Modern Clothes"),
]

# (애니명, template_animation_id, action_description, directions)
ANIMS_HANBOK = [
    ("idle",  "breathing-idle",   None, ["east"]),
    ("walk",  "walking-6-frames", None, ["east"]),
    ("run",   "running-6-frames", None, ["east"]),
    ("jump",  "jumping-1",        None, ["east"]),
    # 템플릿에 climbing이 없다 -> v3 커스텀. 사다리는 뒷모습(north)이 관습.
    ("climb", None, "climbing a ladder, hands and feet alternating on the rungs", ["north"]),
]
# 현대복은 세종 만나기 전 평지 구간 전용 (사다리·점프·단차 없음) -> idle/walk 2종만
ANIMS_MODERN = [
    ("idle", "breathing-idle",   None, ["east"]),
    ("walk", "walking-6-frames", None, ["east"]),
]


def load():
    return json.load(open(IDS)) if os.path.exists(IDS) else {}


def save(d):
    json.dump(d, open(IDS, "w"), indent=1)


def base():
    d = load()
    for key, desc, name in CHARS:
        if d.get(key):
            print(f"skip {key} (exists {d[key]})")
            continue
        r = call_tool("create_character", dict(
            description=desc, name=name, view="side", n_directions=4, size=96,
            mode="standard", outline="single color outline", shading="basic shading",
            detail="high detail"))
        txt = tool_text(r)
        cid = None
        for line in txt.splitlines():
            if line.strip().lower().startswith("id:"):
                cid = line.split(":", 1)[1].strip()
        d[key] = cid
        print(f"{key}: {cid}")
        if not cid:
            print("  RAW:", txt[:400])
        save(d)
        time.sleep(15)


def anim():
    d = load()
    for key, anims in (("seonbi_hanbok", ANIMS_HANBOK), ("player_modern", ANIMS_MODERN)):
        cid = d.get(key)
        if not cid:
            print(f"no base for {key}")
            continue
        for aname, tid, adesc, dirs in anims:
            args = dict(character_id=cid, animation_name=aname, directions=dirs)
            if tid:
                args["template_animation_id"] = tid
            else:
                args["action_description"] = adesc
                args["mode"] = "v3"
                args["frame_count"] = 4
            r = call_tool("animate_character", args)
            print(f"{key}/{aname}:", tool_text(r)[:160].replace("\n", " | "))
            time.sleep(15)


def fetch():
    d = load()
    for key, cid in d.items():
        r = call_tool("get_character", {"character_id": cid})
        txt = tool_text(r)
        with open(os.path.join(OUT, f"_char_{key}.txt"), "w", encoding="utf-8") as f:
            f.write(txt)
        status = [l for l in txt.splitlines() if l.lower().startswith(("status", "animations"))]
        print(f"== {key} {cid}: {status}")


if __name__ == "__main__":
    {"base": base, "anim": anim, "fetch": fetch}[sys.argv[1]]()
