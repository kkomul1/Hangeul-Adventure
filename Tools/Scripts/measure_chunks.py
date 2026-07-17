# 지형 청크의 표면선 y를 실측해 chunk_manifest.json에 기록 (위험 #3: seed 없음 -> 측정+정렬이 유일한 해법)
# 표면선 = 각 열에서 처음으로 불투명해지는 y. 좌/우 edge 높이 일치 여부가 이음새 판정 기준.
import json, os, sys
from PIL import Image

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main"
MAN = os.path.join(OUT, "chunk_manifest.json")


def surface_profile(path):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    a = im.load()
    prof = []
    for x in range(w):
        top = None
        for y in range(h):
            if a[x, y][3] > 128:
                top = y
                break
        prof.append(top if top is not None else h)
    return w, h, prof


def report(path):
    w, h, prof = surface_profile(path)
    name = os.path.basename(path)
    left, right = prof[0], prof[-1]
    solid_bottom = True
    im = Image.open(path).convert("RGBA")
    a = im.load()
    gaps = sum(1 for x in range(w) if a[x, h - 1][3] < 128)
    return {
        "file": name, "w": w, "h": h,
        "surface_left": left, "surface_right": right,
        "edge_delta": abs(left - right),
        "surface_min": min(prof), "surface_max": max(prof),
        "bottom_transparent_cols": gaps,
    }


if __name__ == "__main__":
    entries = {}
    if os.path.exists(MAN):
        entries = json.load(open(MAN))
    for p in sys.argv[1:]:
        r = report(p)
        entries[r["file"]] = r
        print(json.dumps(r, indent=1))
    with open(MAN, "w") as f:
        json.dump(entries, f, indent=1)
    print("-> chunk_manifest.json updated")
