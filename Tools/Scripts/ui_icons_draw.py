# UI 아이콘 벡터 드로잉 (플랫 실루엣 계열) — gear.png 규격(64x64)에 맞춘 직접 작화
# lock  : 붕어 자물쇠 실루엣, 흰색 RGB + 알파만 = 코드의 img.color 틴트가 정확히 먹는다
# ruby  : 커션컷 루비, 색을 구워 넣는다 (틴트 안 함)
# usage: ui_icons_draw.py
import os
from PIL import Image, ImageDraw

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\ui_icons"
os.makedirs(OUT, exist_ok=True)

S = 8          # 슈퍼샘플 배율
N = 64         # 최종 크기 (gear.png와 동일)
C = N * S

RUBY_MAIN = (235, 51, 61)     # #EB333D  게임 루비색
RUBY_LIGHT = (255, 122, 130)
RUBY_DARK = (150, 26, 38)
RUBY_DEEP = (99, 18, 28)
RUBY_LINE = (74, 26, 26)      # 어두운 warm brown 계열 아웃라인 (순수 검정 아님)
SPARK = (255, 235, 235)


def sc(pts):
    return [(x * S, y * S) for x, y in pts]


def down(img):
    return img.resize((N, N), Image.LANCZOS)


# ---------------- lock: 붕어 자물쇠 실루엣 ----------------
def draw_lock():
    img = Image.new("RGBA", (C, C), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    W = (255, 255, 255, 255)

    # U자 고리(shackle): "잠김"을 전달하는 핵심 요소 → 굵고 크게. 몸통 중심 위에 얹는다.
    d.rounded_rectangle(sc([(19, 8), (41, 34)]), radius=11 * S, fill=W)
    # 고리 안쪽 구멍 — 아래로 길게 뚫어 U자를 명확히
    d.rounded_rectangle(sc([(25, 14), (35, 37)]), radius=5 * S, fill=(0, 0, 0, 0))

    # 붕어 몸통: 가로로 누운 통통한 타원 (조선 붕어 자물쇠의 특징)
    d.ellipse(sc([(5, 29), (51, 57)]), fill=W)
    # 머리 쪽 뭉툭하게 보강 (왼쪽)
    d.ellipse(sc([(4, 34), (20, 52)]), fill=W)
    # 꼬리 지느러미 (오른쪽) — 몸통과 겹쳐 하나의 실루엣으로. 크고 단순하게
    d.polygon(sc([(44, 43), (60, 32), (60, 54)]), fill=W)

    # 열쇠구멍: 뚫어서(alpha=0) 자물쇠임을 명확히. 고리 중심(30)에 정렬
    d.ellipse(sc([(25, 36), (35, 46)]), fill=(0, 0, 0, 0))
    d.polygon(sc([(27.5, 43), (32.5, 43), (34, 53), (26, 53)]), fill=(0, 0, 0, 0))

    return down(img)


# ---------------- ruby: 커션컷 보석 ----------------
def draw_ruby():
    img = Image.new("RGBA", (C, C), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # 실루엣 기준점
    TL, TR = (19, 15), (45, 15)          # 테이블(윗면) 좌우
    SL, SR = (7, 27), (57, 27)           # 어깨(가장 넓은 지점)
    BOT = (32, 57)                       # 아래 꼭짓점

    # 아웃라인 (실루엣을 살짝 키워서 깔아준다)
    d.polygon(sc([(TL[0] - 1.5, TL[1] - 1.5), (TR[0] + 1.5, TR[1] - 1.5),
                  (SR[0] + 1.5, SR[1]), (BOT[0], BOT[1] + 1.5), (SL[0] - 1.5, SL[1])]),
              fill=RUBY_LINE + (255,))

    # 크라운(윗면) — 밝은 본색
    d.polygon(sc([TL, TR, SR, SL]), fill=RUBY_MAIN + (255,))
    # 테이블 상단 하이라이트 밴드
    d.polygon(sc([TL, TR, (41, 22), (23, 22)]), fill=RUBY_LIGHT + (255,))
    # 좌측 어깨 면
    d.polygon(sc([TL, (23, 22), (18, 27), SL]), fill=RUBY_MAIN + (255,))
    # 우측 어깨 면 — 그늘 (빛은 왼쪽에서)
    d.polygon(sc([TR, (41, 22), (46, 27), SR]), fill=RUBY_DARK + (255,))

    # 파빌리온(아랫면) — 좌/우 명암 분리
    d.polygon(sc([SL, (32, 27), BOT]), fill=RUBY_DARK + (255,))
    d.polygon(sc([SR, (32, 27), BOT]), fill=RUBY_DEEP + (255,))
    # 중앙 파빌리온 밝은 쐐기 (보석 느낌의 핵심)
    d.polygon(sc([(20, 27), (44, 27), BOT]), fill=RUBY_MAIN + (255,))
    d.polygon(sc([(27, 27), (37, 27), BOT]), fill=RUBY_DARK + (255,))

    # 스파클 하이라이트 (좌상단)
    d.polygon(sc([(21.5, 16.5), (27, 16.5), (24.5, 21), (19.5, 21)]), fill=SPARK + (255,))

    return down(img)


if __name__ == "__main__":
    draw_lock().save(os.path.join(OUT, "lock_draw.png"))
    draw_ruby().save(os.path.join(OUT, "ruby_draw.png"))
    print("saved lock_draw.png, ruby_draw.png")
