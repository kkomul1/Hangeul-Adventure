"""Place adopted props + ground chunks into Assets/Resources/Art/Forest.  0 generations.

Writes each PNG plus a minimal, correct .meta so the sprite is usable even before the main runs
ForestImportTools. Settings mirror that tool's output:
    textureType Sprite(8), PPU 64, filterMode Point(0), spriteMode Single(1), alignment Custom(9)
    props / man-made / plants -> pivot (0.5, 0)      BottomCenter (crop base = foot)
    ground chunks             -> pivot (0.5, 0.6062) surface line 63/160 (== ground_flat_03)

Runtime loads these via Resources.Load<Sprite>(path) (ArtLibrary), which resolves by path, not by
GUID -- so a fresh GUID per file is fine and no id needs to stay stable. A minimal meta omits the
sprite sub-table on purpose: Unity generates the single sprite from spriteMode+pivot on import, and
ForestImportTools re-applies everything idempotently when the main runs it. Nothing here compiles
or triggers a refresh; the files sit inert until the main reimports.
"""
import hashlib
import os
import shutil
import subprocess
import sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import polish_select as SEL
import chunk_adopt as CA

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
POL = os.path.join(ROOT, "ArtDrop", "Generated", "forest_polish")
DST = os.path.join(ROOT, "Assets", "Resources", "Art", "Forest")

META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 9
  spritePivot: {{x: 0.5, y: {pivy}}}
  spritePixelsToUnits: 64
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteGenerateFallbackPhysicsShape: 1
  textureType: 8
  textureShape: 1
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def existing_guid(rel):
    """Preserve the GUID of a prop that already exists (the 5 regens overwrite the flimsy ones).
    Even though nothing references these by GUID today (runtime is path-based), keeping the id
    stable avoids churn and any chance of a dangling reference. Fall back to git HEAD if the
    working meta was already replaced, then to a deterministic derived id for genuinely new files.
    """
    meta = os.path.join(DST, rel.replace("/", os.sep) + ".png.meta")
    # git HEAD first: it holds the committed original, which a prior run of this script may have
    # already overwritten in the working tree with a derived id.
    for text in (
        _git_show("Assets/Resources/Art/Forest/" + rel + ".png.meta"),
        open(meta).read() if os.path.exists(meta) else "",
    ):
        for line in text.splitlines():
            if line.startswith("guid:"):
                return line.split(":", 1)[1].strip()
    return hashlib.md5(("hangeul-forest-polish:" + rel).encode()).hexdigest()


def _git_show(path):
    try:
        return subprocess.run(["git", "show", "HEAD:" + path], cwd=ROOT,
                              capture_output=True, text=True).stdout
    except Exception:
        return ""


def place(src, rel, pivy):
    dst = os.path.join(DST, rel.replace("/", os.sep) + ".png")
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    guid = existing_guid(rel)
    shutil.copyfile(src, dst)
    with open(dst + ".meta", "w", newline="\n") as f:
        f.write(META.format(guid=guid, pivy=("%.4f" % pivy)))
    return rel, Image.open(dst).size


def main():
    placed = []
    # props -> BottomCenter
    pf = os.path.join(POL, "props", "final")
    for name in SEL.PICKS:
        src = os.path.join(pf, name + ".png")
        if os.path.exists(src):
            placed.append(place(src, "Props/" + name, 0.0))
    # ground chunks -> surface pivot 0.6062 (all adopted chunks have surface_median 63)
    tf = os.path.join(POL, "terrain", "final")
    for name, _src, _why in CA.ADOPT:
        src = os.path.join(tf, name + ".png")
        if os.path.exists(src):
            placed.append(place(src, "Terrain/" + name, 0.6062))

    print("placed %d assets into Assets/Resources/Art/Forest:" % len(placed))
    for rel, size in placed:
        print("  %-40s %s" % (rel, size))
    return placed


if __name__ == "__main__":
    main()
