# Poll map objects listed in a json file {name: id} and download PNGs to lookcheck_b2
import base64, json, os, sys, time
from mcp import call_tool, tool_text

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\lookcheck_b2"
os.makedirs(OUT, exist_ok=True)

ids_file = sys.argv[1] if len(sys.argv) > 1 else "b2_batch1_ids.json"
with open(ids_file) as f:
    MAP_OBJECTS = json.load(f)

def save_first_image(res, path):
    for p in res.get("result", {}).get("content", []):
        if p.get("type") == "image":
            with open(path, "wb") as f:
                f.write(base64.b64decode(p["data"]))
            return True
    return False

pending = dict(MAP_OBJECTS)
deadline = time.time() + 600
while pending and time.time() < deadline:
    for name, oid in list(pending.items()):
        r = call_tool("get_map_object", {"object_id": oid})
        txt = tool_text(r)
        if save_first_image(r, os.path.join(OUT, name + ".png")):
            print(f"SAVED {name}.png")
            del pending[name]
        elif "failed" in txt.lower() or "error" in txt.lower():
            print(f"FAILED {name}: {txt[:300]}")
            del pending[name]
        time.sleep(2)
    if pending:
        print("waiting...", list(pending))
        time.sleep(12)
print("done. remaining:", list(pending))
