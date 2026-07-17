# SideWorld.Build.cs가 그릴 결과를 Assets/ 실제 파일 + 코드 상수/피벗으로 재현한다.
# 목적: 컴파일 없이 (a) 피벗 규약 (b) 이음새 (c) 사다리 수정 (d) 정렬 순서를 눈으로 검증.
import json, math, os
from PIL import Image

ROOT = r"C:\Users\minjae\UnityProjects\HangeulAdventure"
A = os.path.join(ROOT, r"Assets\Resources\Art\Forest")
PPU = 64
W, H = 1280, 720
TOP_W = 9.625  # 카메라 중심 y=4.0, orthoSize 5.625

# --- ForestImportTools의 피벗 표 (정규화, 좌하단 원점) ---
GROUND_PIVOT_Y = {"ground_flat_02": 0.5687, "ground_flat_03": 0.6062, "ground_flat_04": 0.4813}
TOPLEFT = {"step_mound_a", "step_mound_b", "platform_earth_mid", "platform_earth_cap_L", "bg_ridge"}

# --- SideWorld.Build.cs 상수 ---
SurfaceY = 0.5
GroundPitch = 210 / 64
GroundChunkW = 320 / 64
GroundOverhang = 4.0
BackfillTopY = SurfaceY - 24 / 64
BackfillBottomY = -8.0
MoundLiftY = 4 / 64
PlatformLiftY = 40 / 64
PlatformCapW = 96 / 64
PlatformMidW = 160 / 64
BushOverlapY = 6 / 64
LadderCapSinkY = 6 / 64
DecoSinkY = 3 / 64
TreeBaseY = SurfaceY - 8 / 64
RidgeTopY = 6.0
RidgeParallax = 0.25
FogTopY = 4.2
SignLabelY = 0.93
TreeLargeMinWidth = 7
GROUND_CHUNKS = ["ground_flat_03", "ground_flat_02", "ground_flat_04"]
DIRT = (125, 91, 80, 255)
CHAR_FOOT = {"SeonbiHanbok": 0.1176, "PlayerModern": 0.0956}

cache = {}
def spr(group, name):
    k = (group, name)
    if k not in cache:
        cache[k] = Image.open(os.path.join(A, group, name + ".png")).convert("RGBA")
    return cache[k]

def pivot(group, name, im):
    if name in GROUND_PIVOT_Y: return (0.5, GROUND_PIVOT_Y[name])
    if name in TOPLEFT: return (0.0, 1.0)
    if name == "sky_gradient": return (0.5, 0.5)
    if name == "fog_band": return (0.5, 1.0)
    return (0.5, 0.0)

class Canvas:
    def __init__(self, x0):
        self.x0 = x0
        self.layers = []   # (order, seq, img, px, py)
        self.seq = 0
    def place(self, group, name, wx, wy, order, flip=False, scale=None):
        im = spr(group, name)
        pv = pivot(group, name, im)
        if scale: im = im.resize((max(1,round(im.width*scale[0])), max(1,round(im.height*scale[1]))), Image.NEAREST)
        if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)
        # 피벗 -> 좌상단 픽셀 (flip은 피벗 기준 미러 = 피벗 x를 1-x로)
        pvx = (1 - pv[0]) if flip else pv[0]
        px = round((wx - self.x0) * PPU - pvx * im.width)
        py = round((TOP_W - wy) * PPU - (1 - pv[1]) * im.height)
        self.layers.append((order, self.seq, im, px, py)); self.seq += 1
    def rect(self, x0w, x1w, y0w, y1w, color, order):
        w = max(1, round((x1w - x0w) * PPU)); h = max(1, round((y0w - y1w) * PPU))
        im = Image.new("RGBA", (w, h), color)
        px = round((x0w - self.x0) * PPU); py = round((TOP_W - y0w) * PPU)
        self.layers.append((order, self.seq, im, px, py)); self.seq += 1
    def render(self):
        out = Image.new("RGBA", (W, H), (0, 0, 0, 255))
        for order, seq, im, px, py in sorted(self.layers, key=lambda t: (t[0], t[1])):
            out.alpha_composite(im, (px, py))
        return out.convert("RGB")

m = json.load(open(os.path.join(ROOT, r"Assets\Resources\Maps\map_101_side.json"), encoding="utf-8"))
pf = [l["terrain"] for l in m["layers"] if l["type"] == "playfield"][0]
bdl = [l for l in m["layers"] if l["type"] == "backdrop"][0]
bd = bdl["terrain"]
MH, MW = len(pf), len(pf[0])
def tile(g, x, y): return g[len(g) - 1 - y][x] if 0 <= x < len(g[0]) and 0 <= y < len(g) else '.'

def merge_solid():
    rects, open_ = [], {}
    for y in range(MH):
        nxt, x = {}, 0
        while x < MW:
            if tile(pf, x, y) != '#': x += 1; continue
            x0 = x
            while x < MW and tile(pf, x, y) == '#': x += 1
            w = x - x0
            if (x0, w) in open_:
                r = list(open_.pop((x0, w))); r[3] += 1; nxt[(x0, w)] = tuple(r)
            else: nxt[(x0, w)] = (x0, y, w, 1)
        rects += list(open_.values()); open_ = nxt
    return rects + list(open_.values())

def clusters():
    skip = [all(tile(bd, x, y) != '.' for x in range(MW)) for y in range(MH)]
    seen, out = set(), []
    for y in range(MH):
        if skip[y]: continue
        for x in range(MW):
            if (x, y) in seen or tile(bd, x, y) == '.': continue
            st, pts = [(x, y)], []
            seen.add((x, y))
            while st:
                p = st.pop(); pts.append(p)
                for d in ((1,0),(-1,0),(0,1),(0,-1)):
                    nx, ny = p[0]+d[0], p[1]+d[1]
                    if not (0 <= nx < MW and 0 <= ny < MH): continue
                    if skip[ny] or (nx,ny) in seen or tile(bd, nx, ny) == '.': continue
                    seen.add((nx,ny)); st.append((nx,ny))
            xs = [p[0] for p in pts]
            out.append((min(xs), max(xs)-min(xs)+1))
    out.sort(); return out

def section(x0, idx):
    c = Canvas(x0)
    camx = min(max(x0 + 10, 10 - 0.5), MW - 0.5 - 10)  # ClampToMap 근사
    center = ((MW - 1) * 0.5, (MH - 1) * 0.5)

    # 하늘 (카메라 중심, 시야 폭까지 가로 스케일)
    sky = spr("Backdrop", "sky_gradient")
    c.place("Backdrop", "sky_gradient", camx, 4.0, -100, scale=((20*PPU+128)/sky.width, 1))
    # 능선 (parallax 0.25 루트 오프셋)
    roff = (camx - center[0]) * (1 - RidgeParallax)
    rw = spr("Backdrop", "bg_ridge").width / PPU
    for i in range(math.ceil((MW + 40) / rw)):
        c.place("Backdrop", "bg_ridge", -20 + i * rw + roff, RidgeTopY, -90)
    # 안개
    fg = spr("Backdrop", "fog_band")
    c.place("Backdrop", "fog_band", camx, FogTopY, -80, scale=((20*PPU+128)/fg.width, 1))
    # 배경 나무 (parallax 0.5)
    toff = (camx - center[0]) * (1 - bdl["parallax"])
    for (mx, w) in clusters():
        c.place("Backdrop", "tree_pine_large" if w >= TreeLargeMinWidth else "tree_pine_small",
                mx + (w-1)*0.5 + toff, TreeBaseY, -10)

    mounds = []
    for (x, y, w, h) in merge_solid():
        if y == 0 and w >= 8:
            left, right = x - 0.5 - GroundOverhang, x + w - 0.5 + GroundOverhang
            c.rect(left, right, BackfillTopY, BackfillBottomY, DIRT, -2)
            n = math.ceil((right - left) / GroundPitch) + 1
            for i in range(n):
                c.place("Terrain", GROUND_CHUNKS[i % 3], left + GroundChunkW*0.5 + i*GroundPitch,
                        SurfaceY, -1, flip=(i % 2 == 1))
        elif w <= 2 and h >= 3:
            sp = spr("Terrain", "boundary_bush"); step = sp.height/PPU - BushOverlapY
            b = y - 0.5
            while b < y + h - 0.5:
                c.place("Terrain", "boundary_bush", x + (w-1)*0.5, b, 2); b += step
        else: mounds.append((x, y, w, h))
    mounds.sort()
    for i, (x, y, w, h) in enumerate(mounds):
        c.place("Terrain", "step_mound_a" if i % 2 == 0 else "step_mound_b",
                x - 0.5, y + h - 0.5 + MoundLiftY, 0)

    # 원웨이 발판
    for y in range(MH):
        x = 0
        while x < MW:
            def ow(xx):
                t = tile(pf, xx, y)
                return t == '=' or (t == 'H' and (tile(pf, xx-1, y) in '=#' or tile(pf, xx+1, y) in '=#'))
            if not ow(x): x += 1; continue
            x0_ = x
            while x < MW and ow(x): x += 1
            w = x - x0_
            left, right, topY = x0_ - 0.5, x0_ + w - 0.5, y + 0.5 + PlatformLiftY
            c.place("Terrain", "platform_earth_cap_L", left, topY, 0)
            c.place("Terrain", "platform_earth_cap_L", right, topY, 0, flip=True)
            px = left + PlatformCapW
            while px < right - PlatformCapW:
                c.place("Terrain", "platform_earth_mid", px, topY, 0); px += PlatformMidW

    # 사다리 (수정된 적층)
    for x in range(MW):
        y = 0
        while y < MH:
            if tile(pf, x, y) != 'H': y += 1; continue
            y0 = y
            while y < MH and tile(pf, x, y) == 'H': y += 1
            bY, tY = y0 - 0.5, y - 0.5
            segH = spr("Terrain", "ladder_body_seg").height / PPU
            b = bY
            while b + segH <= tY + 0.001:
                c.place("Terrain", "ladder_body_seg", x, b, 1); b += segH
            c.place("Terrain", "ladder_body_seg", x, tY - segH, 1)
            c.place("Terrain", "ladder_top_cap", x, tY - LadderCapSinkY, 1)

    # 데코
    for d in m["decorations"]:
        c.place("Props", d["art"], d["x"], d.get("y", 0.5) - DecoSinkY, 2, flip=d.get("flip", False))
    # 스팟 팻말 / 출구 문
    for s in m["spots"]:
        c.place("Props", "spot_sign_hanji", s["pos"][0], (MH - 1 - s["pos"][1]) - 0.5, 3)
    for e in m["exits"]:
        c.place("Props", "exit_gate_jangseung", e["pos"][0], (MH - 1 - e["pos"][1]) - 0.5, 3)
    # 캐릭터 (한복 기본)
    ch = Image.open(os.path.join(A, "Char", "SeonbiHanbok", "Walk.png")).convert("RGBA")
    fr = ch.crop((2*136, 0, 3*136, 136))
    fx = round((14.0 - x0) * PPU - 0.5*136)
    fy = round((TOP_W - 0.5) * PPU - (1 - CHAR_FOOT["SeonbiHanbok"]) * 136)
    c.layers.append((5, 9999, fr, fx, fy))
    climb = Image.open(os.path.join(A, "Char", "SeonbiHanbok", "Climb.png")).convert("RGBA").crop((2*136,0,3*136,136))
    cx = round((47.0 - x0) * PPU - 0.5*136)
    cy = round((TOP_W - 3.2) * PPU - (1 - CHAR_FOOT["SeonbiHanbok"]) * 136)
    c.layers.append((5, 10000, climb, cx, cy))

    out = os.path.join(os.path.dirname(__file__), f"unitypath_{idx}_x{x0}-{x0+20}.png")
    c.render().save(out); print("saved", out)

for i, x0 in enumerate([0, 20, 40]):
    section(x0, i + 1)
