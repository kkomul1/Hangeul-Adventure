# 파생 에셋 로컬 생성 (생성 0회).
# ★ 판단 근거: step_mound / platform_cap 은 3회 재롤해도 생성기가 "캔버스를 꽉 채운 솔리드 블록"을
#   못 그렸다(풀 띠만, 아이소메트릭 큐브 등). 이 둘은 결국 "지상과 같은 흙을 2u 올린 것"이므로
#   성공한 지상 청크에서 크롭해 만드는 편이 재질 일치가 보장되고 확실하다. 생성기와 싸우지 않는다.
import json, os
from PIL import Image

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\forest_main"
MAN = json.load(open(os.path.join(OUT, "chunk_manifest.json")))


def load(n):
    return Image.open(os.path.join(OUT, n + "_final.png")).convert("RGBA")


def pad_down(im, target_h):
    """마지막 불투명 행을 아래로 반복해 캔버스를 채운다 (흙 몸체 연장)."""
    w, h = im.size
    out = Image.new("RGBA", (w, target_h), (0, 0, 0, 0))
    out.alpha_composite(im, (0, 0))
    px = out.load()
    for x in range(w):
        last = None
        for y in range(h):
            if px[x, y][3] > 128:
                last = px[x, y]
        if last:
            for y in range(h, target_h):
                px[x, y] = last
    return out


def step_mound(src, x0, name, w=256, h=128):
    """지상 청크에서 4u x 2u 단차 블록을 크롭. 상단 = 걷는 면."""
    im = load(src)
    surf = MAN[src]["surface_median"]
    top = max(0, surf - 4)                      # 풀 프린지 4px 여유
    crop = im.crop((x0, top, x0 + w, min(top + h, im.height)))
    if crop.height < h:
        crop = pad_down(crop, h)
    crop.save(os.path.join(OUT, name + "_final.png"))
    print(f"{name}: {src}[{x0}..{x0+w}, y{top}..] -> {crop.size}")
    return crop


if __name__ == "__main__":
    # 단차 블록 2종 — 서로 다른 청크/구간에서 떠서 반복 티 방지
    step_mound("ground_flat_03_320x160", 32, "derived_step_mound_a_256x128")
    step_mound("ground_flat_04_320x160", 40, "derived_step_mound_b_256x128")

    # 발판 끝 캡: platform_earth_mid 의 좌측 끝(자연스러운 테이퍼)을 그대로 캡으로 쓴다.
    mid = load("platform_earth_mid_192x96")
    bb = mid.split()[3].getbbox()
    cap = mid.crop((bb[0], 0, bb[0] + 96, 96))
    cap.save(os.path.join(OUT, "derived_platform_cap_L_96x96_final.png"))
    print("derived_platform_cap_L: from platform_earth_mid left taper", cap.size)
    print("done (0 generations)")
