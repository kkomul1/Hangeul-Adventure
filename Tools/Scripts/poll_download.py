# Retry ladder, poll all jobs, download PNGs to ArtDrop/Generated/lookcheck_b/
import base64, json, os, time, urllib.request
from mcp import call_tool, tool_text, rpc

OUT = r"C:\Users\minjae\UnityProjects\HangeulAdventure\ArtDrop\Generated\lookcheck_b"
os.makedirs(OUT, exist_ok=True)

STYLE = "Korean Joseon dynasty folk style, ink-wash inspired muted palette, dark sepia outline"

CHAR_ID = "a06be2ac-0e59-4732-83df-a2170b3d1ced"
MAP_OBJECTS = {
    "platform_forest_192x96": "75b13cfa-e921-4050-81b6-5cfca2a4a311",
    "bg_ridge_200x120": "34e1045a-e509-4f8b-ab51-5c4867e361fb",
    "prop_seokdeung_64x96": "34c3572d-3eea-44e6-b4fe-3795e644c7ea",
    "prop_jangseung_64x128": "d32f6e81-2574-47a6-b163-94f4b281a268",
}

def extract_images(res):
    """Return list of (mime, bytes) from MCP content image parts."""
    out = []
    for p in res.get("result", {}).get("content", []):
        if p.get("type") == "image":
            out.append((p.get("mimeType", "image/png"), base64.b64decode(p["data"])))
    return out

def save_first_image(res, path):
    imgs = extract_images(res)
    if imgs:
        with open(path, "wb") as f:
            f.write(imgs[0][1])
        return True
    return False

# 1. retry ladder (was rate-limited)
ladder_id = None
for attempt in range(6):
    r = call_tool("create_map_object", {
        "description": ("simple traditional wooden ladder, two straight vertical side rails spanning the full "
                        "height of the canvas from top edge to bottom edge, evenly spaced horizontal rungs, "
                        "vertically repeatable tile pattern, weathered brown wood lashed with straw rope, " + STYLE),
        "width": 48, "height": 128,
        "view": "side",
        "outline": "single color outline",
        "shading": "basic shading",
        "detail": "medium detail",
    })
    txt = tool_text(r)
    if "rate limit" in txt.lower():
        print(f"ladder attempt {attempt}: rate limited, waiting 30s")
        time.sleep(30)
        continue
    print("LADDER:", txt.splitlines()[0])
    for line in txt.splitlines():
        if line.startswith("id:"):
            ladder_id = line.split("id:")[1].strip()
    break
if ladder_id:
    MAP_OBJECTS["ladder_wood_48x128"] = ladder_id

# 2. poll map objects
pending = dict(MAP_OBJECTS)
deadline = time.time() + 600
results = {}
while pending and time.time() < deadline:
    for name, oid in list(pending.items()):
        r = call_tool("get_map_object", {"object_id": oid})
        txt = tool_text(r)
        if save_first_image(r, os.path.join(OUT, name + ".png")):
            print(f"SAVED {name}.png")
            results[name] = "ok"
            del pending[name]
        elif "failed" in txt.lower() or "error" in txt.lower():
            print(f"FAILED {name}: {txt[:300]}")
            results[name] = "failed: " + txt[:200]
            del pending[name]
        time.sleep(2)
    if pending:
        time.sleep(10)

# 3. poll character
char_done = False
deadline = time.time() + 600
while not char_done and time.time() < deadline:
    r = call_tool("get_character", {"character_id": CHAR_ID})
    txt = tool_text(r)
    low = txt.lower()
    if "processing" in low:
        print("character still processing...")
        time.sleep(20)
        continue
    # completed: dump text (contains rotation URLs / download link) and any inline images
    with open(os.path.join(OUT, "_character_info.txt"), "w", encoding="utf-8") as f:
        f.write(txt)
    imgs = extract_images(r)
    for i, (mime, data) in enumerate(imgs):
        with open(os.path.join(OUT, f"char_seonbi_inline_{i}.png"), "wb") as f:
            f.write(data)
    print("CHARACTER DONE. inline images:", len(imgs))
    print(txt[:1500])
    char_done = True

print("\nbalance check:")
print(tool_text(call_tool("get_balance")))
