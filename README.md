# Action Renamer

Dalamud plugin for FINAL FANTASY XIV. Gives skills, traits, job names and status effects a sci-fi theme, lets you set your own skill names and tooltip descriptions, and installs a matching sci-fi icon pack through Penumbra. Everything is client-side.

## Install

1. In game, open `/xlsettings` → **Experimental** → **Custom Plugin Repositories**.
2. Add `https://raw.githubusercontent.com/Dhrubo95ZD/ActionRenamer/main/repo.json`, tick **Enabled**, and save.
3. Open `/xlplugins`, search **Action Renamer**, and install.
4. For the icons, have **Penumbra** installed. The plugin installs and enables the icon mod on first start. Log out and back in to see the icons.

Settings: `/actionrenamer`.

## Release a new version

```bash
./release.sh 1.2.0
```

Rebuilds the icon pack and the plugin, bumps `repo.json`, pushes, and creates the GitHub release that Dalamud downloads.
