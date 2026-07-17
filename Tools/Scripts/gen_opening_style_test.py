# -*- coding: utf-8 -*-
"""오프닝 컷1(현대의 혼란)을 두 화풍으로 생성해 비교용으로 저장.

- 모드 C (픽셀): 참조 = ArtDrop/Generated/lookcheck_b2/scene_composite_1280.png (인게임 룩)
- 모드 A (페인팅): 참조 = ArtDrop/title_art.jpg (타이틀 키아트)
두 화풍 모두 같은 모델을 써서 비교가 '모델 차이'가 아닌 '화풍 차이'만 반영하게 한다.

키는 런타임에 파일에서 읽는다 (코드/인자에 리터럴 금지).
사용: python gen_opening_style_test.py [pixel|painting|both]
"""
import base64, json, os, sys, urllib.request, urllib.error

KEY = open(os.path.expanduser("~/.claude/secrets/gemini-api-key.txt")).read().strip()
BASE = "https://generativelanguage.googleapis.com/v1beta"
MODEL = "models/gemini-2.5-flash-image"

ROOT = r"C:/Users/minjae/UnityProjects/HangeulAdventure"
OUTDIR = ROOT + "/ArtDrop/Generated/opening_style_test"

REF_PIXEL = ROOT + "/ArtDrop/Generated/lookcheck_b2/scene_composite_1280.png"
REF_PAINT = ROOT + "/ArtDrop/title_art.jpg"

PROMPT_PIXEL = """Panel 1 of 4 in a pixel art cutscene series for a game, 16:9 landscape.

Match the PIXEL STYLE of the attached reference: 16-bit pixel art, chunky clearly
visible pixels, crisp pixel clusters, limited palette, no anti-aliased smooth
gradients, dark warm brown single-color outline (never pure black), cel shading
with one shadow step and one highlight step, gentle mid-value contrast, soft
directional light from the LEFT, eye-level camera. Match the reference's pixel
density and outline treatment exactly. Do NOT match the reference's palette --
this panel uses a different palette, described below.

Palette for THIS panel -- deliberately cold and wrong: dead concrete gray, sickly
pale cyan-white fluorescent light, wet asphalt blue-black, and living black ink.
NO mossy green, NO warm earth brown, almost no warm accent -- a world drained of
life. Tranquil, quiet dread, not loud action.

Scene: a modern city street at dawn, eye level. Shop signboards, a bookshop window
display, a road sign and a bus stop board are all COMING APART -- the marks on them
crack into small angular geometric fragments that peel off, tumble through the air,
and dissolve into curling tendrils of glossy black ink smoke. Loose book pages and
leaflets blow down the street, their surfaces already blank. In the middle ground,
ordinary modern people stand frozen and helpless, mouths open, gesturing at one
another -- speech has stopped working; from one or two of them, small broken
fragments drift out of their mouths and crumble. A young modern man in a gray
hoodie and dark jeans stands left of center with his back to us, small against the
street, staring up at a disintegrating signboard -- the only one still trying to read.

Composition: strong left-to-right depth, the street receding toward the right.
Keep the bottom fifth calm for a dialogue text box.

Absolutely no text, letters, numbers, or letter-like shapes anywhere -- every sign,
book and page carries only abstract angular marks or blank surfaces."""

PROMPT_PAINT = """Panel 1 of 4 in a game's opening cutscene, 16:9 landscape.

Use the EXACT same art style as the attached reference illustration: highly
detailed painterly digital painting, rich visible brushwork, intricate textures,
dramatic cinematic lighting, strong atmospheric depth, layered
foreground-midground-background composition. Match the reference's brushwork,
lighting drama and level of detail identically.

This is the ONE panel of the opening that is NOT set in Joseon -- it is the modern
world. So shift the palette away from the reference: make it cold and drained --
dead concrete gray, sickly pale cyan-white fluorescent light, wet asphalt
blue-black, and living black ink. The reference's persimmon-orange warmth is almost
entirely absent here; the world has had its life bleached out. Keep the mood
tranquil and ominous -- quiet dread, not loud action.

Scene: a modern city street at dawn, eye level. Shop signboards, a bookshop window
display, a road sign and a bus stop board are all COMING APART -- the marks on them
crack into small angular geometric fragments that peel off, tumble through the air
and dissolve into curling tendrils of glossy black ink smoke, exactly like the
drifting cracked fragments and black ink tendrils in the reference image. Loose book
pages and leaflets blow down the street, their surfaces already blank. In the middle
ground, ordinary modern people stand frozen and helpless, mouths open, gesturing at
one another -- speech has stopped working. A young modern man in a gray hoodie and
dark jeans stands left of center with his back to us, small against the street,
staring up at a disintegrating signboard -- the only one still trying to read,
echoing the lone figure seen from behind in the reference.

Composition: strong left-to-right depth with the street receding to the right, soft
directional light from the LEFT. Keep the bottom fifth calm for a dialogue text box.

Absolutely no text, letters, numbers or letter-like shapes anywhere -- every sign,
book and page carries only abstract angular marks or blank surfaces."""

JOBS = {
    "pixel":    (REF_PIXEL, "image/png",  PROMPT_PIXEL, OUTDIR + "/cut1_pixel.png"),
    "painting": (REF_PAINT, "image/jpeg", PROMPT_PAINT, OUTDIR + "/cut1_painting.png"),
}


def generate(name):
    ref_path, mime, prompt, out = JOBS[name]
    ref = base64.b64encode(open(ref_path, "rb").read()).decode()
    payload = {
        "contents": [{"parts": [
            {"inline_data": {"mime_type": mime, "data": ref}},
            {"text": prompt},
        ]}],
        "generationConfig": {
            "responseModalities": ["IMAGE"],
            "imageConfig": {"aspectRatio": "16:9"},
        },
    }
    req = urllib.request.Request(
        BASE + "/" + MODEL + ":generateContent",
        data=json.dumps(payload).encode(),
        headers={"Content-Type": "application/json", "x-goog-api-key": KEY},
    )
    try:
        res = json.loads(urllib.request.urlopen(req, timeout=180).read())
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", "replace")
        print("[%s] HTTP %s\n%s" % (name, e.code, body))
        return False

    for part in res.get("candidates", [{}])[0].get("content", {}).get("parts", []):
        if "inlineData" in part:
            open(out, "wb").write(base64.b64decode(part["inlineData"]["data"]))
            print("[%s] OK -> %s (%d KB)" % (name, out, os.path.getsize(out) // 1024))
            return True
    print("[%s] no image part. response:\n%s" % (name, json.dumps(res)[:800]))
    return False


if __name__ == "__main__":
    os.makedirs(OUTDIR, exist_ok=True)
    which = sys.argv[1] if len(sys.argv) > 1 else "both"
    targets = list(JOBS) if which == "both" else [which]
    ok = all(generate(t) for t in targets)
    sys.exit(0 if ok else 1)
