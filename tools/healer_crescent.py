#!/usr/bin/env python3
"""Paints a crescent over the healer role mark.

Usage: healer_crescent.py <dir of extracted game PNGs> <repo icons dir>
Input names: lfg.png, img01_lfg.png..img04_lfg.png, contentsfinder.png, lfgselectrole.png,
gcarmyexpedition.png (hr1 sheets) and i62582.png (role icon).
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SRC, ICONS = sys.argv[1], sys.argv[2]
SS = 6  # supersampling


def crescent(size):
    """Cream crescent with a dark edge, like the game's other role marks; opening faces upper right."""
    S = size * SS
    mask = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(mask)
    r = S * 0.42
    c = S / 2
    d.ellipse((c - r, c - r, c + r, c + r), fill=255)
    cx, cy, ir = c + S * 0.17, c - S * 0.12, S * 0.345
    d.ellipse((cx - ir, cy - ir, cx + ir, cy + ir), fill=0)
    m = np.asarray(mask, float) / 255
    # Vertical gradient from cream to warm tan.
    t = np.linspace(0, 1, S)[:, None, None]
    rgb = (1 - t) * np.array([250, 240, 214.0]) + t * np.array([186, 156, 112.0])
    body = np.dstack([np.broadcast_to(rgb, (S, S, 3)), m * 255]).astype("uint8")
    body = Image.fromarray(body, "RGBA")
    edge = mask.filter(ImageFilter.GaussianBlur(S * 0.035))
    out = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    out.paste((22, 34, 16, 255), (0, 0), edge.point(lambda v: min(255, int(v * 1.5))))
    out.alpha_composite(body)
    return out.resize((size, size), Image.LANCZOS)


def fill_tile(a, x0, y0, x1, y1, colors=None):
    """Replaces the box with a clean vertical gradient between the tile's own top and bottom colours."""
    top, bottom = (np.array(c, float) for c in colors) if colors else (a[y0 + 3, x0 + 3].copy(), a[y1 - 4, x0 + 3].copy())
    for y in range(y0, y1):
        f = (y - y0) / max(1, y1 - y0 - 1)
        a[y, x0:x1] = top * (1 - f) + bottom * f


def paint(img, box, framed=True, keep=None, colors=None):
    x0, y0, x1, y1 = box
    a = np.asarray(img, float).copy()
    saved = a[keep[1]:keep[3], keep[0]:keep[2]].copy() if keep else None
    if framed:
        fill_tile(a, x0, y0, x1, y1, colors)
    else:
        a[y0:y1, x0:x1] = 0
    img = Image.fromarray(a.round().astype("uint8"), "RGBA")
    w, h = x1 - x0, y1 - y0
    size = int(min(w, h) * 0.86)
    img.alpha_composite(crescent(size), (x0 + (w - size) // 2, y0 + (h - size) // 2))
    if keep:
        b = np.asarray(img).copy()
        b[keep[1]:keep[3], keep[0]:keep[2]] = saved
        img = Image.fromarray(b, "RGBA")
    return img


def sheet(name, game_path, tiles):
    img = Image.open(os.path.join(SRC, name + ".png")).convert("RGBA")
    for t in tiles:
        img = paint(img, *t)
    out = os.path.join(ICONS, "uld_replacements", game_path + ".png")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    img.save(out)


GREEN = ((58, 140, 28, 255), (24, 58, 22, 255))  # tiles whose corners cannot be sampled
LFG = [((64, 169, 105, 210),), ((60, 220, 112, 272), False)]
sheet("lfg", "ui/uld/LFG_hr1", LFG)
for theme in ("img01", "img02", "img03", "img04"):
    sheet(f"{theme}_lfg", f"ui/uld/{theme}/LFG_hr1", LFG)
sheet("contentsfinder", "ui/uld/ContentsFinder_hr1", [
    ((107, 11, 182, 85),),
    ((62, 166, 105, 210),),
    ((46, 221, 76, 251), True, (62, 235, 80, 254)),  # small tile; keeps the blue upgrade arrow
])
sheet("lfgselectrole", "ui/uld/LFGSelectRole_hr1", [((63, 63, 106, 107), True, None, GREEN)])
sheet("gcarmyexpedition", "ui/uld/GcArmyExpedition_hr1", [((336, 592, 392, 647), True, None, GREEN)])

for theme, prefix in (("", "ui/uld/"), ("img01_", "ui/uld/img01/"), ("img02_", "ui/uld/img02/"), ("img03_", "ui/uld/img03/"), ("img04_", "ui/uld/img04/")):
    if os.path.exists(os.path.join(SRC, f"{theme}togglebutton.png")):
        sheet(f"{theme}togglebutton", f"{prefix}ToggleButton_hr1", [((143, 1, 178, 42), False)])  # Character window role headers
    if os.path.exists(os.path.join(SRC, f"{theme}configcharacterhud.png")):
        sheet(f"{theme}configcharacterhud", f"{prefix}ConfigCharacterHud_hr1", [((45, 5, 75, 35),)])
    if os.path.exists(os.path.join(SRC, f"{theme}buddy.png")):
        sheet(f"{theme}buddy", f"{prefix}Buddy_hr1", [((8, 120, 48, 160),)])

role = Image.open(os.path.join(SRC, "i62582.png")).convert("RGBA")
paint(role, (9, 8, 55, 56)).save(os.path.join(ICONS, "replacements", "062582.png"))
