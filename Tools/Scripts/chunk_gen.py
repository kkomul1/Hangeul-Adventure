"""Ground chunk runner: generate 400x160 -> crop flush 320x160 -> align -> normalise soil -> gate.

See polish_chunks.py for why we generate wide and crop, and chunk_soil.py for why the soil must be
normalised to DirtColor. Every roll is measured; only chunks that pass chunk_measure's gate are
reported as adoptable.

usage: chunk_gen.py <rolls_per_variant>
"""
import base64
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mcp import call_tool, tool_text
import polish_chunks as C
import polish_prompts as P
from chunk_crop import crop_align
from chunk_soil import normalize
from chunk_measure import measure
from forest_post import has_baked_bg, key_corner

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish", "terrain")
RAW = os.path.join(OUT, "raw")
os.makedirs(RAW, exist_ok=True)
IDS = os.path.join(OUT, "_ids.jsonl")


def queue(name, args):
    for attempt in range(6):
        r = call_tool("create_map_object", args)
        txt = tool_text(r)
        if "rate limit" in txt.lower():
            time.sleep(30)
            continue
        for line in txt.splitlines():
            if line.strip().lower().startswith("id:"):
                return line.split(":", 1)[1].strip()
        print("  NO ID", name, txt[:200])
        return None
    return None


def poll(ids, timeout=1500):
    pending = dict(ids)
    saved = []
    deadline = time.time() + timeout
    while pending and time.time() < deadline:
        for name, oid in list(pending.items()):
            r = call_tool("get_map_object", {"object_id": oid})
            got = False
            for p in r.get("result", {}).get("content", []):
                if p.get("type") == "image":
                    with open(os.path.join(RAW, name + ".png"), "wb") as f:
                        f.write(base64.b64decode(p["data"]))
                    saved.append(name)
                    got = True
                    break
            if got:
                del pending[name]
            elif "failed" in tool_text(r).lower():
                del pending[name]
            time.sleep(1.5)
        if pending:
            time.sleep(10)
    return saved


def process(name):
    """raw -> KEY the baked sky -> crop flush 320 window -> align to 63 -> soil -> gate.

    ★ Keying is not optional and must come FIRST. At 400px wide the background always bakes
      (measured: 24/24 raw chunks came back fully opaque, alpha min 255). Skipping the key makes
      every measurement meaningless: surface_profile takes the first opaque pixel per column, so
      with a baked sky it returns y=0 everywhere -- every chunk then reports edge_delta=0,
      seam_delta=0 and sails through the gate. That is exactly how 24/24 "passed" while some were
      literally sunset landscape paintings. A gate that cannot fail is not a gate.
    """
    src = os.path.join(RAW, name + ".png")
    keyed = os.path.join(RAW, name + "_keyed.png")
    if has_baked_bg(src):
        key_corner(src, keyed)
    else:
        keyed = src
    dst = os.path.join(OUT, name + ".png")
    info = crop_align(keyed, dst)
    if not info:
        return name, None, "no flush 320 window"
    normalize(dst)
    r = measure(dst)
    r.update(sanity(dst))
    if not r["accept"] or not r["is_ground"]:
        os.rename(dst, os.path.join(OUT, "rejected_" + name + ".png"))
        return name, r, "gate" if r["accept"] else "seam"
    return name, r, "ACCEPT"


def sanity(path):
    """Is this actually a ground cross-section, or a landscape that happens to have a flat top?

    The seam gate only looks at the two edge columns, so scenery sails through it. Measured
    escapes on the first pass: distant hills baked above the horizon (gc_roots__r2), a teal ridge
    band (gc_pebbles__r0), and two tiny human figures standing on the grass (gc_moss__r0).

    Three requirements, each tied to what a ground chunk actually is:
      body_fill  the mass below the surface is opaque everywhere -- real soil, not a painted vista
      warm       that mass is earth-coloured (R > B); sky and haze are cool (B >= R)
      surf_std   the surface line is FLAT. This is the one that catches scenery: ground_flat_03
                 measures std 1.7 across its width, while anything with hills or a horizon on it
                 spikes far higher. Tolerance 4.0 = a little slack over the shipped tile.
    """
    from PIL import Image
    import numpy as np
    from chunk_crop import surface_profile
    a = np.array(Image.open(path).convert("RGBA"))
    op = a[..., 3] > 128
    h, w = op.shape
    body = op[90:]                       # well below the surface line (63)
    filled = body.mean()
    rgb = a[90:, :, :3][body].astype(float)
    if not len(rgb):
        return dict(is_ground=False, body_fill=0.0, warm=0.0, surf_std=99.0)
    warm = float((rgb[:, 0] > rgb[:, 2]).mean())
    prof = surface_profile(op).astype(float)
    prof = prof[prof < h]
    std = float(prof.std()) if len(prof) else 99.0
    return dict(is_ground=bool(filled > 0.98 and warm > 0.85 and std <= 4.0),
                body_fill=round(float(filled), 3), warm=round(warm, 3),
                surf_std=round(std, 2))


def main(rolls):
    jobs = []
    for cname, desc, _, _ in C.prompts():
        for i in range(rolls):
            n = f"{cname}__r{i}"
            if os.path.exists(os.path.join(RAW, n + ".png")):
                continue
            jobs.append((n, dict(description=desc, width=C.W, height=C.H, **P.COMMON)))
    print(f"{len(jobs)} chunk rolls to generate")
    for i in range(0, len(jobs), 6):
        ids = {}
        for n, a in jobs[i:i + 6]:
            oid = queue(n, a)
            if oid:
                ids[n] = oid
                print("  queued", n)
            time.sleep(6)
        with open(IDS, "a") as f:
            f.write(json.dumps(ids) + "\n")
        poll(ids)

    print("\n--- crop + align + soil + gate ---")
    ok = []
    for f in sorted(os.listdir(RAW)):
        if not f.endswith(".png"):
            continue
        n = f[:-4]
        name, r, why = process(n)
        if why == "ACCEPT":
            ok.append(name)
            print("  ACCEPT %-18s edge_d=%d seam_d=%.1f median=%d" % (
                name, r["edge_delta"], r["seam_delta"], r["surface_median"]))
        else:
            print("  reject %-18s (%s)" % (name, why))
    print(f"\n{len(ok)} chunks passed: {ok}")


if __name__ == "__main__":
    main(int(sys.argv[1]) if len(sys.argv) > 1 else 3)
