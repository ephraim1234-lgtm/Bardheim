# Bardheim 0.7.0 Deployment Checklist

Use this checklist to publish Bardheim `0.7.0` to Thunderstore and Nexus Mods
from the standalone public repository.

Contact for both platforms: `longhouselistings@gmail.com`

## 1. Repository Readiness

- Confirm the local repo is `C:\dev\Bardheim Release`.
- Confirm the remote is `https://github.com/ephraim1234-lgtm/Bardheim.git`.
- Confirm the current branch is `main`.
- Confirm there are no private lab docs, local sounds, session artifacts,
  local r2modman profiles, generated `dist/` output, or untracked DLLs in the
  repo.
- Confirm these public docs exist and are current:
  - `README.md`
  - `LICENSE`
  - `SUPPORT.md`
  - `PUBLISHING.md`
  - `DEPLOYMENT_CHECKLIST.md`
  - `package/thunderstore/README.md`
  - `package/thunderstore/CHANGELOG.md`
  - `package/thunderstore/manifest.json`
- Confirm the license/permissions posture is intentional:
  - Source-available, all rights reserved.
  - Permission and takedown contact is `longhouselistings@gmail.com`.
  - Nexus permissions should stay restrictive unless the owner deliberately
    changes the license.

## 2. Final Build and Test Verification

Run from the repo root:

```powershell
dotnet build src\Bardheim\Bardheim.csproj /p:RestoreSources=https://api.nuget.org/v3/index.json
dotnet build tests\Bardheim.Tests\Bardheim.Tests.csproj /p:RestoreSources=https://api.nuget.org/v3/index.json
dotnet vstest tests\Bardheim.Tests\bin\Debug\net472\Bardheim.Tests.dll
```

Expected result:

- Bardheim project build exits `0`.
- Test project build exits `0`.
- `dotnet vstest` reports `208` passing tests and no failures.

Also verify:

- Built DLL version is `0.7.0`.
- Built DLL embeds:
  - `27` lyre samples.
  - `16` drum samples.
  - `22` flute samples.
- Built DLL SHA256 is recorded for the release notes.
- Known accepted smoke-test DLL SHA256:

```text
F640F9F16DCD345AACAD83FEB551564C87DD09CF4E45E1391CEAB727B73B95C3
```

## 3. Manual Game Smoke Test

Use the r2modman `valheimbot` profile or the release target profile.

- Install `Bardheim.dll` to:

```text
%APPDATA%\r2modmanPlus-local\Valheim\profiles\valheimbot\BepInEx\plugins\Bardheim\Bardheim.dll
```

- Launch Valheim through the same mod manager profile.
- Confirm BepInEx loads Bardheim `0.7.0`.
- Confirm the Workbench recipes appear:
  - Lyre: `10 Wood`, `2 Leather scraps`
  - Drum: `8 Wood`, `4 Leather scraps`, `2 Deer hide`
  - Flute: `6 Wood`, `1 Deer hide`
- Craft or spawn each instrument.
- Equip lyre, toggle play mode with `LeftAlt + P`, and play `1-8`.
- Press `9` on lyre and confirm tuning cycles.
- Equip drum, toggle play mode, and play `1-8`.
- Equip flute, toggle play mode, and play `1-8`.
- Add at least one `.mid` file to:

```text
BepInEx/plugins/Bardheim/songs/
```

- Confirm `O`, `[`, `]`, `-`, `=`, `0`, and `L` work for MIDI playback.
- Confirm `Ctrl + F3` hides/shows the Valheim HUD and Bardheim overlay.
- Confirm multiplayer audio remains disabled by default.
- Check `BepInEx/LogOutput.log` for Bardheim load errors or repeated warnings.

## 4. Thunderstore Package Assembly

Create a fresh package staging folder outside the repo or under ignored
`dist/`.

Expected zip root:

```text
icon.png
manifest.json
README.md
CHANGELOG.md
BepInEx/plugins/Bardheim/Bardheim.dll
```

Copy files:

- `package/thunderstore/icon.png` to `icon.png`
- `package/thunderstore/manifest.json` to `manifest.json`
- `package/thunderstore/README.md` to `README.md`
- `package/thunderstore/CHANGELOG.md` to `CHANGELOG.md`
- Built release DLL to `BepInEx/plugins/Bardheim/Bardheim.dll`

Do not include:

- Source code.
- Test binaries.
- `.pdb` files unless intentionally publishing debug symbols.
- `assets-src/`.
- `runtime-assets/`.
- Local `BepInEx/config` files.
- Local songs.
- Local r2modman profile files.
- Private lab artifacts.
- Any extra DLLs from BepInEx, Jotunn, Valheim, Unity, or other mods.

## 5. Thunderstore Pre-Upload Validation

- Confirm `icon.png` is exactly `256x256` PNG.
- Confirm `manifest.json` is valid JSON.
- Confirm `manifest.json` fields:
  - `name`: `Bardheim`
  - `version_number`: `0.7.0`
  - `website_url`: `https://github.com/ephraim1234-lgtm/Bardheim`
  - `description`: `Playable lyre, drum, and flute instruments for Valheim with local MIDI song playback.`
  - `dependencies`:
    - `denikson-BepInExPack_Valheim-5.4.2333`
    - `ValheimModding-Jotunn-2.29.0`
- Confirm all package metadata files are at the zip root, not inside a nested
  parent folder.
- Preview `README.md` with Thunderstore Markdown Preview.
- Validate `manifest.json` with Thunderstore Manifest Validator.
- Import the zip as a local mod in r2modman or Thunderstore Mod Manager.
- Launch the local imported package and repeat the smoke test if the packaged
  DLL differs from the previously tested DLL.

## 6. Thunderstore Upload

- Log in to Thunderstore.
- Select the intended publishing team.
- Upload the Bardheim `0.7.0` zip.
- Keep package name `Bardheim`.
- Select accurate categories only:
  - `Mods`
  - `Audio`
  - `Client-side`
  - `Utility`
  - `Bog Witch Update` only if it matches the verified current Valheim line.
- Do not select:
  - `Server-side`
  - `Modpacks`
  - `AI Generated`, unless a policy review determines the mod was created
    primarily using AI tools.
  - `Deprecated`
  - `NSFW`
- After upload, open the direct listing link.
- Confirm the listing renders the README, dependencies, version, icon, website
  URL, and changelog correctly.
- Confirm install through r2modman or Thunderstore Mod Manager once the package
  is available or by direct listing/local import while caches update.
- Record the Thunderstore package URL in release notes.

## 7. Nexus Mods File Preparation

Use the same tested `Bardheim.dll`.

Recommended Nexus archive contents:

```text
BepInEx/plugins/Bardheim/Bardheim.dll
README.md
CHANGELOG.md
LICENSE
SUPPORT.md
```

Do not bundle BepInExPack Valheim or Jotunn. List them as requirements instead.

Do not include:

- Thunderstore `manifest.json`.
- Thunderstore-only `icon.png`, unless using it as a Nexus image asset.
- Source tree.
- Test binaries.
- `.pdb` files unless intentionally publishing debug symbols.
- Local config, songs, profile data, or private artifacts.
- Valheim, Unity, BepInEx, or Jotunn DLLs.

## 8. Nexus Mods Page Setup

Recommended fields:

- Game: `Valheim`
- Mod name: `Bardheim`
- Version: `0.7.0`
- Contact: `longhouselistings@gmail.com`
- Short summary:

```text
Playable lyre, drum, and flute instruments for Valheim with local MIDI song playback.
```

- Requirements:
  - BepInExPack Valheim
  - Jotunn
- Category: choose the closest available Valheim category for audio, gameplay,
  or utilities at upload time.
- Tags, if available and accurate:
  - Audio
  - Gameplay
  - Quality of Life
  - Utilities for Players
  - BepInEx
  - Jotunn
- Permissions:
  - Upload permission: restricted.
  - Modification permission: restricted.
  - Conversion permission: restricted.
  - Asset use permission: restricted.
  - Commercial use: not allowed.
  - Donation point permission: not allowed for derivative asset use unless the
    owner changes the license.
- Credits:
  - Bardheim contributors for code and project assets.
  - BepInExPack Valheim and Jotunn as dependencies, not bundled assets.
  - Valheim belongs to Iron Gate Studio.

## 9. Nexus Mods Description

Use this page structure:

```markdown
# Bardheim

Playable lyre, drum, and flute instruments for Valheim.

## Features

## Requirements

## Installation

## Crafting

## Controls

## MIDI Songs

## Multiplayer Status

## Known Limitations

## Credits and Permissions

## Changelog

## Contact
```

Required content:

- State that `0.7.0` focuses on local play.
- State that multiplayer audio is experimental, both-sides-required, and
  disabled by default.
- Include manual install path:

```text
BepInEx/plugins/Bardheim/Bardheim.dll
```

- Include MIDI folder:

```text
BepInEx/plugins/Bardheim/songs/
```

- Include controls.
- Include recipes.
- Include `longhouselistings@gmail.com` for release, permission, and takedown
  contact.
- Link the GitHub repo:

```text
https://github.com/ephraim1234-lgtm/Bardheim
```

## 10. Nexus Mods Upload

- Upload the prepared Nexus archive.
- Mark it as the main file for version `0.7.0`.
- Add a clear file name, for example `Bardheim-0.7.0`.
- Add file description:

```text
Bardheim 0.7.0 for BepInEx/Jotunn. Adds craftable playable lyre, drum, and flute instruments with local MIDI song playback.
```

- Add changelog from `package/thunderstore/CHANGELOG.md`.
- Attach screenshots or images if available:
  - Inventory icon.
  - Instrument equipped.
  - Song overlay.
- If screenshots are not ready, publish without misleading placeholder images.
- Confirm virus scan/status completes.
- Confirm Requirements tab shows BepInExPack Valheim and Jotunn.
- Confirm permissions and credits render correctly.
- Record the Nexus Mods URL in release notes.

## 11. Post-Publish Verification

For both platforms:

- Download or install the published artifact through the platform.
- Confirm the downloaded archive contains only intended files.
- Confirm the DLL hash matches the uploaded release artifact, or record the
  final platform-downloaded hash if platform packaging changes archive metadata.
- Install into a clean mod manager profile.
- Launch Valheim.
- Confirm Bardheim loads.
- Smoke test lyre, drum, flute, MIDI playback, and `Ctrl + F3`.
- Confirm dependency installation works through the platform/mod manager.
- Confirm public docs show:
  - Requirements.
  - Install path.
  - MIDI folder.
  - Multiplayer limitation.
  - Contact email.
  - GitHub URL.

## 12. Announcement and Records

- Create a GitHub release or release note for `0.7.0`, if desired.
- Include:
  - Thunderstore URL.
  - Nexus Mods URL.
  - DLL SHA256.
  - Tested Valheim/BepInEx/Jotunn versions.
  - Summary of local play scope.
  - Multiplayer disabled-by-default note.
  - Contact email.
- Pin or save platform URLs in any project notes used for future updates.
- Watch platform comments/posts after release for install problems.

## 13. If Something Goes Wrong

Thunderstore:

- Uploaded versions are immutable. Fixes require bumping `version_number` and
  uploading a new version.
- If the package is rejected, inspect for accidental extra files, nested zip
  root, invalid icon size, invalid manifest JSON, wrong dependency strings, or
  bundled files from other mods.
- If the mod does not appear in managers immediately, wait for cache propagation
  and use the direct listing/local import for immediate testing.

Nexus Mods:

- If a file or permission setting is wrong, update the page or upload a fixed
  file according to Nexus' file-management UI.
- If rights or takedown concerns are raised, use `longhouselistings@gmail.com`
  as the contact path and preserve the report details.
- If users report dependency issues, confirm they installed BepInExPack Valheim
  and Jotunn in the same profile as Bardheim.

## 14. Next Update Rules

- Do not reuse `0.7.0` for changed packages.
- Increment all version fields together:
  - `src/Bardheim/Plugin.cs`
  - `src/Bardheim/Bardheim.csproj`
  - `package/thunderstore/manifest.json`
  - `package/thunderstore/CHANGELOG.md`
  - docs that mention the current release version.
- Keep Thunderstore package name `Bardheim`.
- Re-run build, tests, package import, and smoke test for every release.
