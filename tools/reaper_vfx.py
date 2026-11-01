#!/usr/bin/env python3
"""Blanks Reaper effect textures that hold circles, rune text, emblems or the hooded figure.

Usage: reaper_vfx.py <index.tsv from the texture dump> <repo icons dir>
index.tsv columns: number, game path, WxH, abilities. Writes one transparent PNG per matched path to
<icons>/vfx_replacements/<game path>.png; the IconPack tool turns those into .atex files.
"""
import os
import re
import sys

from PIL import Image

INDEX, ICONS = sys.argv[1], sys.argv[2]
FLAGGED = re.compile(r"/rrp_(ab_mag\d+x|mag_text\d+x|synchro\d+x|ava\d+x|barrier\d+x|charge\d+x)\.atex$", re.I)

count = 0
for line in open(INDEX, encoding="utf-8"):
    parts = line.rstrip("\n").split("\t")
    if len(parts) < 4 or not FLAGGED.search(parts[1]):
        continue
    width, height = map(int, parts[2].split("x"))
    out = os.path.join(ICONS, "vfx_replacements", parts[1] + ".png")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    Image.new("RGBA", (width, height), (0, 0, 0, 0)).save(out)
    count += 1
print(count, "textures blanked")
