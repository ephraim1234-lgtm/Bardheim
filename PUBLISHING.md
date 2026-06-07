# Bardheim Publishing Checklist

This checklist captures the release-facing work for Bardheim `0.7.0` from the
standalone public repository.

For the full platform-by-platform deployment runbook, see
`DEPLOYMENT_CHECKLIST.md`.

## Research Summary

Checked on 2026-06-07.

### Thunderstore

- A valid package must have `icon.png`, `README.md`, and `manifest.json` at the
  zip root. `CHANGELOG.md` is supported and should stay in the package.
- `icon.png` must be a 256x256 PNG.
- `manifest.json` controls the package name, version, website URL, description,
  and dependency strings. Dependencies declared there are installed by mod
  managers.
- Version strings are three-part semantic versions. Uploaded versions are
  immutable, so README fixes require a new upload.
- For BepInEx games, keep plugin files under `BepInEx/plugins/...` in the
  package so mod managers route them correctly.
- Thunderstore's Valheim browser exposes category filters such as `Mods`,
  `Audio`, `Client-side`, `Utility`, `Bog Witch Update`, and `AI Generated`.
  Use only categories that accurately describe the release.

Sources:

- https://wiki.thunderstore.io/mods/creating-a-package
- https://wiki.thunderstore.io/mods/updating-a-package
- https://wiki.thunderstore.io/mods/packaging-your-mods
- https://wiki.thunderstore.io/mods/mod-not-visible
- https://thunderstore.io/c/valheim/?included_categories=24&ordering=most-downloaded

### Nexus Mods

- Popular Valheim Nexus pages commonly include a short "About this mod" summary,
  requirements, permissions/credits, changelog, install notes, compatibility or
  multiplayer notes, and support guidance.
- Nexus file submission guidance puts the burden on the uploader to have rights
  for submitted content and to credit or obtain permission where required.
- Nexus Terms of Service grant Nexus and site users limited rights around hosted
  content when a file is uploaded, so publishing there is a deliberate license
  and distribution decision.
- For Bardheim, the Nexus page should explicitly list BepInExPack Valheim and
  Jotunn as requirements, describe the manual `BepInEx/plugins/Bardheim/`
  install path, and use restrictive permissions unless the owner chooses a more
  permissive license.

Sources:

- https://help.nexusmods.com/article/28-file-submission-guidelines
- https://help.nexusmods.com/article/18-terms-of-service
- https://www.nexusmods.com/valheim/mods/387
- https://www.nexusmods.com/valheim/mods/1138
- https://www.nexusmods.com/valheim/mods/2323

## Thunderstore Package

Expected zip root:

```text
icon.png
manifest.json
README.md
CHANGELOG.md
BepInEx/plugins/Bardheim/Bardheim.dll
```

Before upload:

- Confirm `package/thunderstore/icon.png` is 256x256 PNG.
- Confirm `package/thunderstore/manifest.json` has:
  - `name`: `Bardheim`
  - `version_number`: `0.7.0`
  - `website_url`: `https://github.com/ephraim1234-lgtm/Bardheim`
  - dependencies:
    - `denikson-BepInExPack_Valheim-5.4.2333`
    - `ValheimModding-Jotunn-2.29.0`
- Preview `package/thunderstore/README.md` in Thunderstore's Markdown Preview.
- Validate `manifest.json` with Thunderstore's Manifest Validator.
- Import the built zip as a local mod in r2modman or Thunderstore Mod Manager
  before upload.
- Upload under the intended Thunderstore team. Do not change the package name
  for later updates.

Suggested categories:

- `Mods`
- `Audio`
- `Client-side`
- `Utility`
- `Bog Witch Update` if the verified Valheim/Jotunn build target is the current
  public game line.

Do not select `Server-side` for `0.7.0`; multiplayer audio is experimental and
both-sides-required, not a server-only feature.

## Nexus Mods Page

Recommended page fields:

- Name: `Bardheim`
- Version: `0.7.0`
- Summary: `Playable lyre, drum, and flute instruments for Valheim with local MIDI song playback.`
- Contact: `longhouselistings@gmail.com`
- Category: choose the closest available Valheim category for gameplay, audio,
  or utilities based on the current Nexus category list at upload time.
- Requirements:
  - BepInExPack Valheim
  - Jotunn
- Permissions: keep redistribution, modification, conversion, and asset-use
  permissions restricted unless the owner intentionally chooses a more permissive
  public license.
- Credits: project-owner audio/visual assets; BepInEx and Jotunn as required
  dependencies, not bundled assets.

Suggested description outline:

```markdown
# Bardheim

Adds craftable playable lyre, drum, and flute instruments to Valheim.

## Features

## Requirements

## Installation

## Controls

## MIDI Songs

## Multiplayer Status

## Known Limitations

## Credits and Permissions

## Changelog
```

Manual install text:

```text
Install BepInExPack Valheim and Jotunn first. Extract Bardheim so the DLL lands
at BepInEx/plugins/Bardheim/Bardheim.dll. Launch once to create
BepInEx/plugins/Bardheim/songs/, then add your .mid files there.
```

## Release Verification

Fresh release verification for `0.7.0` from this repository:

- `dotnet build src\Bardheim\Bardheim.csproj /p:RestoreSources=https://api.nuget.org/v3/index.json`
- `dotnet build tests\Bardheim.Tests\Bardheim.Tests.csproj /p:RestoreSources=https://api.nuget.org/v3/index.json`
- `dotnet vstest tests\Bardheim.Tests\bin\Debug\net472\Bardheim.Tests.dll`
- Manual smoke test through r2modman `valheimbot` profile.
- Confirm release DLL SHA256 before uploading.

Known verified DLL SHA256:

```text
F640F9F16DCD345AACAD83FEB551564C87DD09CF4E45E1391CEAB727B73B95C3
```

## License Decision

`LICENSE` is currently all rights reserved/source-available. This is deliberate
for `0.7.0` because the repository includes audio assets with provenance and
redistribution constraints. If the owner wants Bardheim to be open source, pick
the target license explicitly and re-audit every bundled asset before changing
the license file or Nexus permissions.

Use `longhouselistings@gmail.com` for release, permission, and takedown contact.
