#!/usr/bin/env python3
"""Writes icons/file_swaps.json: Reaper effect files that show symbols -> clean effects from other jobs.

Usage: reaper_swaps.py <donors.tsv: name, action, game path> <reaper.tsv: action, pvp, game path, FLAG|ok> <icons dir>
Fails when a flagged effect has no donor or a donor is unknown, so nothing gets silently left out.
"""
import json
import os
import sys

DONORS, REAPER, ICONS = sys.argv[1:4]

# Reaper effect file -> donor effect name.
SWAPS = {
    "2km_ws04_c1x": "ab_2kt001c0t",     # Shadow of Death -> Shinten
    "2km_ws04_t1x": "ab_2kt001t0t",
    "2km_ws07_c1x": "ab_2kt002c0t",     # Whorl of Death -> Kyuten
    "2km_ws07_c4x": "abl_2gb003c0i",
    "2km_ws10_c2x": "ab_2kt001c0t",     # Gibbet
    "2km_ws11_c2x": "ab_2kt001c0t",     # Gallows
    "2km_ws12_c1x": "ab_2kt002c0t",     # Guillotine
    "2km_ws16_c3x": "ab_2kt020c1x",     # Plentiful Harvest -> Guren
    "2km_ws16_c6x": "abl_2gb003c0i",
    "2km_ws19_c0x": "ab_2kt020c1x",     # Executioner's Guillotine
    "2km_ws20_c0x": "ab_2kt020c1x",     # Perfectio
    "2km_ws20_c1x": "ab_2kt002c0t",
    "2km_ws20_c4x": "abl_2gb003c0i",
    "2km_ws20_t0x": "abl_2gb003t0i",
    "mgc_2km001_c0x": "2kt_ws10c0t",    # Harpe -> Enpi
    "mgc_2km002_c1x": "ab_2kt020c1x",   # Communio
    "mgc_2km003_c0x": "abl_2gb006c0i",  # Soulsow -> Aurora
    "ab_2km002_c1x": "ab_2kt002c0t",    # Grim Swathe
    "ab_2km003_c0x": "abl_2gb003c0i",   # Gluttony -> Danger Zone
    "ab_2km003_t0x": "abl_2gb003t0i",
    "ab_2km004_c1x": "abl_2gb001c0i",   # Enshroud -> No Mercy
    "ab_2km010_c0x": "abl_2gb005c0i",   # Arcane Crest -> Nebula
    "ab_2km014_c0x": "abl_2gb003c0i",   # Death Warrant (PvP)
    "ab_2km014_t0x": "abl_2gb003t0i",
    "ab_2km015_t0x": "abl_2gb003t0i",
    "abl_pvpcom029c1x": "abl_2gb001c0i",  # Tenebrae Lemurum (PvP)
    "abl_pvpcom029c2x": "ab_2kt002c0t",
}

donors = {}
for line in open(DONORS, encoding="utf-8"):
    name, _, path = line.rstrip("\n").split("\t")
    donors[name] = path

swaps = {}
for line in open(REAPER, encoding="utf-8"):
    parts = line.rstrip("\n").split("\t")
    if parts[3] != "FLAG":
        continue
    path = parts[2]
    name = path.rsplit("/", 1)[1][:-5]
    if name not in SWAPS:
        sys.exit(f"no donor for flagged effect {path}")
    swaps[path] = donors[SWAPS[name]]

# Effects with no flagged texture that still draw something unwanted (the figure summoned by Blood Stalk).
EXTRA = {
    "vfx/rrp/ab_2km001/eff/ab_2km001_c0x.avfx": "abl_2gb003c0i",
    "vfx/rrp/ab_2km001/eff/ab_2km001_c2x.avfx": "abl_2gb001c1i",
    "vfx/rrp/ab_2km001/eff/ab_2km001_c3x.avfx": "abl_2gb001c0i",
    "vfx/rrp/ab_2km001/eff/ab_2km001_t0x.avfx": "abl_2gb003t0i",
}
for path, donor in EXTRA.items():
    swaps[path] = donors[donor]

with open(os.path.join(ICONS, "file_swaps.json"), "w", encoding="utf-8") as f:
    json.dump(dict(sorted(swaps.items())), f, indent=2)
print(len(swaps), "effect swaps")
