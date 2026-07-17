# 캐릭터 로테이션 + 애니 프레임 다운로드 -> forest_main/char/{key}/{anim}_{n}.png
# _char_{key}.txt(=get_character 출력)에서 URL을 파싱한다. 서버 URL은 만료되므로 즉시 확보.
import json, os, re, sys, urllib.request

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main"
CH = os.path.join(OUT, "char")

URL_RE = re.compile(r"https://backblaze\.pixellab\.ai/[^\s,]+\.png(?:\?t=\d+)?")


def get(url, path):
    try:
        with urllib.request.urlopen(url, timeout=60) as r:
            data = r.read()
        with open(path, "wb") as f:
            f.write(data)
        return True
    except Exception as e:
        print("  FAIL", os.path.basename(path), e)
        return False


def parse(key):
    txt = open(os.path.join(OUT, f"_char_{key}.txt"), encoding="utf-8").read()
    d = os.path.join(CH, key)
    os.makedirs(d, exist_ok=True)
    n = 0
    # 로테이션 (east = 사이드뷰 본체, west는 로컬 미러로 만들면 되므로 0회)
    for m in re.finditer(r"^\s{2}(south|east|north|west): (https://\S+)$", txt, re.M):
        if get(m.group(2), os.path.join(d, f"rot_{m.group(1)}.png")):
            n += 1
    # 애니: "  walk — 1 dir (east), 6f ..." 다음 줄에 "    east: url, url, ..."
    cur = None
    for line in txt.splitlines():
        m = re.match(r"^  (\w+) — .*?\[type=", line)
        if m:
            cur = m.group(1)
            continue
        m = re.match(r"^    (south|east|north|west): (.+)$", line)
        if m and cur:
            for i, u in enumerate(URL_RE.findall(m.group(2))):
                if get(u, os.path.join(d, f"{cur}_{m.group(1)}_{i}.png")):
                    n += 1
    print(f"{key}: {n} files -> {d}")
    return n


if __name__ == "__main__":
    total = sum(parse(k) for k in ["seonbi_hanbok", "player_modern"])
    print("total", total)
