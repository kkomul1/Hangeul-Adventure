# 실패분만 골라 재롤. 기존 산출물은 rerollA_ 접두로 보관(비교·근거 보존).
# usage: forest_reroll.py terrain | props
import os, shutil, sys
import forest_prompts as P
from forest_gen import run, OUT, prop_jobs

TERRAIN_TARGETS = ["ground_flat_03_320x160", "ground_flat_04_320x160", "step_mound_a_256x128",
                   "step_mound_b_256x128", "platform_earth_mid_192x96", "platform_earth_cap_L_96x96",
                   "ladder_body_seg_96x128", "ladder_top_cap_96x64", "boundary_bush_96x256"]
PROP_TARGETS = ["prop_mossy_boulder", "prop_bush_cluster", "prop_fern_tuft", "prop_fallen_log"]


def archive(names, tag="rerollA_"):
    for t in names:
        for suf in ["", "_keyed", "_final"]:
            f = os.path.join(OUT, t + suf + ".png")
            if os.path.exists(f):
                shutil.move(f, os.path.join(OUT, tag + t + suf + ".png"))


if __name__ == "__main__":
    g = sys.argv[1]
    if g == "terrain":
        archive(TERRAIN_TARGETS)
        jobs = [(n, a) for n, a in P.TERRAIN if n in TERRAIN_TARGETS]
    else:
        archive(PROP_TARGETS)
        jobs = [(n, a) for n, a in prop_jobs() if n in PROP_TARGETS]
    print("re-rolling:", [n for n, _ in jobs])
    run(jobs)
