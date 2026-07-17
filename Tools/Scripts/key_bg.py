# Remove baked-in flat background from a generated terrain PNG via corner flood fill.
# usage: key_bg.py <in.png> <out.png> [tolerance]
import sys
from collections import deque
from PIL import Image

src, dst = sys.argv[1], sys.argv[2]
TOL = int(sys.argv[3]) if len(sys.argv) > 3 else 34

im = Image.open(src).convert("RGBA")
w, h = im.size
px = im.load()

def close(c1, c2, tol):
    return all(abs(a - b) <= tol for a, b in zip(c1[:3], c2[:3]))

seen = [[False] * w for _ in range(h)]
q = deque()
for (sx, sy) in [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1), (w // 2, 0)]:
    q.append((sx, sy, px[sx, sy]))
    seen[sy][sx] = True

removed = 0
while q:
    x, y, ref = q.popleft()
    c = px[x, y]
    px[x, y] = (0, 0, 0, 0)
    removed += 1
    for nx, ny in ((x+1, y), (x-1, y), (x, y+1), (x, y-1)):
        if 0 <= nx < w and 0 <= ny < h and not seen[ny][nx]:
            nc = px[nx, ny]
            if nc[3] > 0 and close(nc, c, TOL):
                seen[ny][nx] = True
                q.append((nx, ny, nc))

im.save(dst)
print(f"removed {removed} px ({removed / (w*h):.2%}) -> {dst}")
