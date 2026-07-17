"""Re-poll every object id ever queued and save any PNG still missing on disk.  0 generations.

Objects live 8h server-side but the local runner only saves them during its own poll loop. When a
run dies mid-batch (e.g. the HTTP 502 that killed the prop batch at job 27), the rolls in flight
were already paid for but never written. Their ids are in _ids.jsonl, so they are recoverable for
free -- as long as it happens inside the 8h window.
"""
import base64
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mcp import call_tool, tool_text

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish", "props")
IDS = os.path.join(OUT, "_ids.jsonl")


def main():
    seen = {}
    with open(IDS) as f:
        for line in f:
            line = line.strip()
            if line:
                seen.update(json.loads(line))
    missing = {n: i for n, i in seen.items()
               if not os.path.exists(os.path.join(OUT, n + ".png"))}
    print(f"{len(seen)} ids queued, {len(missing)} missing locally")
    got = 0
    for name, oid in missing.items():
        r = call_tool("get_map_object", {"object_id": oid})
        img = None
        for p in r.get("result", {}).get("content", []):
            if p.get("type") == "image":
                img = p["data"]
                break
        if img:
            with open(os.path.join(OUT, name + ".png"), "wb") as f:
                f.write(base64.b64decode(img))
            print("  RECOVERED", name)
            got += 1
        else:
            print("  gone/failed", name, tool_text(r)[:70].replace("\n", " "))
    print(f"recovered {got}/{len(missing)}")


if __name__ == "__main__":
    main()
