# 한복 선비 복장 불일치 수정: run/jump 재생성 (v3 커스텀).
#
# ★ 원인 (실측):
#   template 모드는 스켈레톤 포즈를 강제한다. running-6-frames / jumping-1 처럼 다리가 크게
#   벌어지는 스켈레톤에서는 모델이 발목 길이 도포를 렌더할 수 없어 "짧은 상의 + 바지"로
#   재해석해 버린다. 반면 climb는 v3 커스텀이라 도포가 그대로 유지됐다 (실측 확인).
#   => v3는 캐릭터 로테이션 이미지를 시작 프레임으로 삼아 정체성을 붙잡는다.
#
# usage: forest_char_costume.py gen | fetch | dl
import json, os, re, sys, time, urllib.request
from mcp import call_tool, tool_text

CID = "f4e82336-e07f-4371-9c50-afc9a6b28328"  # Seonbi Hanbok Forest
OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main"
CH = os.path.join(OUT, "char", "seonbi_hanbok", "Seonbi_Hanbok_Forest", "animations")

# 도포 유지 문구. 스키마상 action_description은 "움직임/포즈만" 권장이지만,
# 복장이 무너지는 게 문제이므로 실루엣 유지를 포즈 서술의 일부로 명시한다.
ROBE = (" The long ankle-length robe stays full-length in every frame, its hem flowing and "
        "billowing but never shortening; the legs stay covered by the robe. The wide-brimmed "
        "black gat hat stays the same large size on his head in every frame.")

JOBS = [
    # (애니명, action_description, frame_count)
    ("run_v2",  "running fast to the right, arms pumping" + ROBE, 6),
    ("jump_v2", "jumping upward: crouching, launching off the ground, rising, then falling" + ROBE, 8),
]


def gen():
    for name, desc, fc in JOBS:
        r = call_tool("animate_character", dict(
            character_id=CID, animation_name=name, action_description=desc,
            mode="v3", frame_count=fc, directions=["east"], keep_first_frame=False))
        print(f"{name}:", tool_text(r)[:200].replace("\n", " | "))
        time.sleep(10)


def fetch():
    r = call_tool("get_character", {"character_id": CID})
    txt = tool_text(r)
    p = os.path.join(OUT, "_char_seonbi_costume.txt")
    open(p, "w", encoding="utf-8").write(txt)
    for l in txt.splitlines():
        if re.match(r"^  \w+ — ", l):
            print(re.sub(r"https://\S+", "<url>", l))
    return txt


URL_RE = re.compile(r"https://backblaze\.pixellab\.ai/[^\s,]+\.png(?:\?t=\d+)?")


def dl():
    txt = open(os.path.join(OUT, "_char_seonbi_costume.txt"), encoding="utf-8").read()
    cur = None
    n = 0
    for line in txt.splitlines():
        m = re.match(r"^  (\w+) — .*?\[type=", line)
        if m:
            cur = m.group(1)
            continue
        m = re.match(r"^    (south|east|north|west): (.+)$", line)
        if m and cur and cur.endswith("_v2"):
            d = os.path.join(CH, cur, m.group(1))
            os.makedirs(d, exist_ok=True)
            for i, u in enumerate(URL_RE.findall(m.group(2))):
                with urllib.request.urlopen(u, timeout=60) as r:
                    open(os.path.join(d, f"frame_{i:03d}.png"), "wb").write(r.read())
                n += 1
            print(f"{cur}/{m.group(1)}: {len(URL_RE.findall(m.group(2)))}f -> {d}")
    print("total", n)


if __name__ == "__main__":
    {"gen": gen, "fetch": fetch, "dl": dl}[sys.argv[1]]()
