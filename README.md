# Action Renamer

Dalamud plugin for FINAL FANTASY XIV. Gives skills, traits, job names and status effects a sci-fi theme, lets you set your own skill names and tooltip descriptions, and installs a matching sci-fi icon pack through Penumbra. Everything is client-side.

## Install

1. In game, open `/xlsettings` → **Experimental** → **Custom Plugin Repositories**.
2. Add `https://raw.githubusercontent.com/Dhrubo95ZD/ActionRenamer/main/repo.json`, tick **Enabled**, and save.
3. Open `/xlplugins`, search **Action Renamer**, and install.
4. For the icons, have **Penumbra** installed. The plugin installs and enables the icon mod on first start. Log out and back in to see the icons.

Settings: `/actionrenamer`.

## Duty helpers

The **Duty** tab in the settings has three switches, all on by default:

- **Press Commence when a duty pops.**
- **Answer Yes to ready checks.**
- **Leave the duty when it is finished and the loot is done.** After the duty completes, the plugin waits for the treasure coffers to be opened (two minutes at most) and for every loot item to be rolled for and handed out, then leaves. It never leaves in combat or during a cutscene, and it only acts in Duty Finder content. It does not roll for you. Tick *Leave as soon as my own rolls are in* to stop waiting for the rest of the party.

To stay in a duty you just finished, type `/actionrenamer stay`.

## Release a new version

```bash
./release.sh 1.2.0
```

Rebuilds the icon pack and the plugin, bumps `repo.json`, pushes, and creates the GitHub release that Dalamud downloads.
