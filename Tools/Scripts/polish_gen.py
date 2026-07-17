"""Prop generation runner for the polish pass. Queue -> poll -> save PNG locally.

Objects auto-delete server-side after 8h, so every result is written to disk immediately.
Usage:
    python Tools/Scripts/polish_gen.py probe                 # 3 props x1, validate the new ref
    python Tools/Scripts/polish_gen.py all <rolls>           # every prop x N rolls
    python Tools/Scripts/polish_gen.py only <name>[,<name>] <rolls>
"""
import base64
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mcp import call_tool, tool_text
import polish_prompts as P

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish", "props")
REF = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish", "ref_forest_192.png")
os.makedirs(OUT, exist_ok=True)


def ref_b64():
    with open(REF, "rb") as f:
        return json.dumps({"type": "base64", "base64": base64.b64encode(f.read()).decode()})


def queue(name, args):
    for attempt in range(6):
        r = call_tool("create_map_object", args)
        txt = tool_text(r)
        if "rate limit" in txt.lower():
            print(f"  {name}: rate limited -> 30s")
            time.sleep(30)
            continue
        for line in txt.splitlines():
            if line.strip().lower().startswith("id:"):
                return line.split(":", 1)[1].strip()
        print(f"  {name}: NO ID :: {txt[:300]}")
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
                    with open(os.path.join(OUT, name + ".png"), "wb") as f:
                        f.write(base64.b64decode(p["data"]))
                    print("  SAVED", name)
                    saved.append(name)
                    got = True
                    break
            if got:
                del pending[name]
            else:
                if "failed" in tool_text(r).lower():
                    print("  FAILED", name)
                    del pending[name]
            time.sleep(1.5)
        if pending:
            print("  waiting:", len(pending), "left")
            time.sleep(10)
    if pending:
        print("  TIMEOUT:", list(pending))
    return saved


def jobs_for(entries, rolls, mode="basic", start_roll=0):
    """mode='basic'   -> standalone object, transparent bg, canvas from the size hierarchy.
       mode='inpaint' -> style-matched against ref_forest_192.

    ★ basic is the default. The probe measured why: inpainting makes a prop absorb whatever
      surrounds the mask. Against the old sky ref the fern came out sky-coloured (spread 33);
      against the thicket ref it came out thicket-shaped (a green mound, not a fern), with the
      rectangular mask edge and a strip of the ref's grass baked into it. Basic mode has no
      donor to absorb, so the silhouette is the object's own -- and palette consistency is
      restored offline by polish_palette.apply(), which cannot be bled into.
    """
    jobs = []
    bg = ref_b64() if mode == "inpaint" else None
    for name, desc, frac, tier in entries:
        w, h = P.canvas_for(name, tier)
        for r in range(start_roll, start_roll + rolls):
            jn = f"{name}__r{r}"
            if mode == "inpaint":
                args = dict(description=desc, background_image=bg,
                            inpainting=json.dumps({"type": "rectangle", "fraction": frac}),
                            **P.COMMON)
            else:
                args = dict(description=desc, width=w, height=h, **P.COMMON)
            jobs.append((jn, args))
    return jobs


def run(jobs, batch=8, resume=True):
    """Queue in batches so a stall never strands the whole run.

    resume: skip any job whose PNG is already on disk. Generations are the scarce resource and
    a re-run must never re-buy a roll it already has. (Learned the hard way: an orphaned nohup
    run and a relaunch went in parallel and each re-queued the same names, burning ~22 rolls.)
    """
    if resume:
        before = len(jobs)
        jobs = [(n, a) for n, a in jobs if not os.path.exists(os.path.join(OUT, n + ".png"))]
        if before != len(jobs):
            print(f"resume: skipping {before - len(jobs)} already on disk, {len(jobs)} to go")
    allsaved = []
    for i in range(0, len(jobs), batch):
        chunk = jobs[i:i + batch]
        ids = {}
        for name, args in chunk:
            oid = queue(name, args)
            if oid:
                ids[name] = oid
                print("queued", name, oid)
            time.sleep(6)
        with open(os.path.join(OUT, "_ids.jsonl"), "a") as f:
            f.write(json.dumps(ids) + "\n")
        allsaved += poll(ids)
    print(f"\n=== saved {len(allsaved)}/{len(jobs)} ===")
    return allsaved


def _single_instance():
    """Refuse to start if another run is live. Two concurrent runs re-queue the same names and
    silently double-spend the generation budget."""
    lock = os.path.join(OUT, "_run.lock")
    if os.path.exists(lock):
        age = time.time() - os.path.getmtime(lock)
        if age < 1800:
            print(f"another run is active (lock {age:.0f}s old). Delete {lock} to override.")
            sys.exit(1)
    with open(lock, "w") as f:
        f.write(str(os.getpid()))
    import atexit
    atexit.register(lambda: os.path.exists(lock) and os.remove(lock))


if __name__ == "__main__":
    mode = sys.argv[1]
    gen = os.environ.get("GENMODE", "basic")
    _single_instance()
    if mode == "probe2":
        sel = [e for e in P.PROPS if e[0] in
               ("prop_fern_tuft", "prop_mossy_boulder", "prop_onggi_jar")]
        run(jobs_for(sel, 1, gen, start_roll=90))
    elif mode == "all":
        run(jobs_for(P.PROPS, int(sys.argv[2]), gen))
    elif mode == "only":
        names = sys.argv[2].split(",")
        sel = [e for e in P.PROPS if e[0] in names]
        sr = int(sys.argv[4]) if len(sys.argv) > 4 else 0
        run(jobs_for(sel, int(sys.argv[3]), gen, start_roll=sr))
    else:
        print("unknown mode")
