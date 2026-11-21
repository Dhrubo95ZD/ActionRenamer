# Action Renamer

Dalamud plugin for FINAL FANTASY XIV. Gives skills, traits, job names and status effects a sci-fi theme, lets you set your own skill names and tooltip descriptions, and installs a matching sci-fi icon pack through Penumbra. Everything is client-side.

## Install

1. In game, open `/xlsettings` → **Experimental** → **Custom Plugin Repositories**.
2. Add `https://raw.githubusercontent.com/Dhrubo95ZD/ActionRenamer/main/repo.json`, tick **Enabled**, and save.
3. Open `/xlplugins`, search **Action Renamer**, and install.
4. For the icons, have **Penumbra** installed. The plugin installs and enables the icon mod on first start. Log out and back in to see the icons.

Settings: `/actionrenamer`.

## Duty helpers

The **Duty helpers** tab in the settings has these switches, all on by default:

- **Press Commence when a duty pops.**
- **Answer Yes to ready checks.**
- **Answer Yes to "Move immediately to sealed area?"**
- **Accept raises.** Answers Yes to "Accept Raise from ...?" when someone raises you.
- **Make Boss Mod follow the tank in duties.** In Duty Finder content the plugin tells Boss Mod to follow the party's tank (`/vbmai follow`), within the distance you set. Boss Mod walks after the tank between fights and dodges on its own during boss fights. When you are the tank, nothing happens; with no tank in the party, it follows the healer. Needs Boss Mod.
- **Leave the duty when it is finished and the loot is done.** After the duty completes, the plugin waits for the treasure coffers to be opened (two minutes at most) and for every loot item to be rolled for and handed out, then leaves. It never leaves in combat or during a cutscene, and it only acts in Duty Finder content. It does not roll for you. Tick *Leave as soon as my own rolls are in* to stop waiting for the rest of the party.

To stay in a duty you just finished, type `/actionrenamer stay`.

## Auto-queue

The **Auto-queue** tab lists every duty the Duty Finder can queue for, grouped like the Duty Finder: dungeons, guildhests, trials, extreme and unreal trials, normal, alliance, savage and ultimate raids, each split by expansion. Duties you have not unlocked are greyed out.

Pick a duty, set the number of runs and press **Start**. The plugin queues through the Duty Finder, and after each run it waits a few seconds and queues again until the runs are done. With *Commence* and *Leave the duty* switched on in Duty helpers, the whole loop runs by itself. Queue alone or as the party leader. Press **Stop** to end it; if you withdraw from the queue yourself while it runs, it queues again.

## My plugins

The **My plugins** tab in the settings lists every plugin I use, shows which are installed, and opens the plugin installer on each missing one. On a new PC, before anything else is installed, use the list below.

1. In game, open `/xlsettings` → **Experimental** → **Custom Plugin Repositories**, add each link, tick **Enabled**, and save.
2. Open `/xlplugins` and install the plugins in the table.
3. In Penumbra's settings, set the mod folder to `C:\FFXIVMods`. Action Renamer installs its icon pack by itself.

Repositories:

- `https://raw.githubusercontent.com/Dhrubo95ZD/ActionRenamer/main/repo.json`
- `https://raw.githubusercontent.com/xivdev/Penumbra/master/repo.json`
- `https://raw.githubusercontent.com/1Stalk/DalamudPlugins/main/pluginmaster.json`
- `https://love.puni.sh/ment.json`
- `https://puni.sh/api/repository/veyn`
- `https://github.com/NightmareXIV/MyDalamudPlugins/raw/main/pluginmaster.json`
- `https://puni.sh/api/repository/ice`
- `https://puni.sh/api/repository/erdelf`
- `https://puni.sh/api/repository/croizat`
- `https://puni.sh/api/repository/aetherlove`
- `https://puni.sh/api/repository/vera`

| Plugin | What it does |
|---|---|
| Action Renamer | Sci-fi names and icons for skills, jobs and status effects. |
| Artisan | A 'simple' crafting plugin. |
| AutoDuty | Dungeons while you sleep, eat, or work. (Swimsuit Shtola made easy.) |
| AutoHook | Makes fishing less (or more) boring |
| AutoKeyboundBrawler | Automated assistant and typing solver for Keybound Brawler. |
| AutoRetainer | Collect and assign ventures to your retainers from the comfort of your bed. |
| Avarice | Positional feedback with pixel perfect rear/flank tracking, range indicators, and more. |
| Boss Mod | Don't fail at raid mechanics anymore |
| Chilled Leves | I heard you like your leves on the rocks. |
| Deliveroo | Better Grand Company Deliveries |
| Ice's Cosmic Exploration | Welcome to, the dark side of, the MOOOON |
| LazyLoot | Dungeon Loot but Lazy. |
| Lifestream | A plugin to speed up Aethernet travel and World changing. |
| Marketbuddy | Less clicks, more gil! |
| Orbwalker | S P A C E G L I D I N G |
| Palantir | Lost in a deep dungeon with nothing but dry bread and water? |
| Pandora's Box | Do you dare to open Pandora's Box? |
| Penumbra | Runtime mod loader and manager. |
| Questionable | A tiny quest helper plugin. |
| Saucy | When working for MGP is too much effort. |
| SomethingNeedDoing | Work work. |
| Splatoon | Scriptable overlay engine for drawing mechanics, markers, and indicators in real-time |
| TextAdvance | Quest speedrunning assistance tool. Automatically skips subtitle boxes and confirms cutscene skipping |
| Visland | Automate island sanctuary activities |
| vnavmesh | Move around! |
| vsatisfy | Simplify custom deliveries. |
| Wrath Combo | Condenses combos and mutually exclusive abilities onto a single button - and then some. |
| YesAlready | Clicks Yes in various dialogs you've specified. |

## Release a new version

```bash
./release.sh 1.2.0
```

Rebuilds the icon pack and the plugin, bumps `repo.json`, pushes, and creates the GitHub release that Dalamud downloads.
