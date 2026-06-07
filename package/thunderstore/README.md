# Bardheim

Bardheim adds craftable playable instruments to Valheim: a lyre, hand drum, and
flute. Equip an instrument, toggle play mode, play manual notes with `1-8`, or
perform local MIDI songs from your plugin folder.

Version `0.7.0` focuses on local play. Experimental multiplayer audio is present
but disabled by default.

## Features

- Craftable lyre, drum, and flute.
- Manual instrument play with `1-8`.
- Local Standard MIDI format 0/1 song playback.
- Song selection, speed control, looping, progress, and in-game overlay.
- Lyre tunings, General MIDI drum mapping, and flute melodic folding.
- Generated in-game instrument visuals and inventory icons.
- `Ctrl + F3` support for hiding the Valheim HUD and Bardheim overlay together.

## Requirements

- BepInExPack Valheim
- Jotunn

Thunderstore-compatible mod managers should install both dependencies
automatically.

## Installation

Install with r2modman, Thunderstore Mod Manager, or another
Thunderstore-compatible manager.

For manual installs, install BepInExPack Valheim and Jotunn first, then place
the plugin at:

```text
BepInEx/plugins/Bardheim/Bardheim.dll
```

## Crafting

All instruments are crafted at a Workbench.

| Instrument | Recipe |
| --- | --- |
| Lyre | `10 Wood`, `2 Leather scraps` |
| Drum | `8 Wood`, `4 Leather scraps`, `2 Deer hide` |
| Flute | `6 Wood`, `1 Deer hide` |

## Controls

| Control | Action |
| --- | --- |
| `LeftAlt + P` | Toggle play mode while holding a Bardheim instrument |
| `1-8` | Play manual notes or drum hits |
| `9` | Cycle lyre tuning |
| `O` | Play or stop the selected MIDI song |
| `[` / `]` | Select previous or next MIDI song |
| `-` / `=` | Decrease or increase song speed |
| `0` | Reset song speed to `1.00x` |
| `L` | Toggle song looping |
| `Ctrl + F3` | Hide or show the Valheim HUD and Bardheim overlay |

## MIDI Songs

Bardheim loads user MIDI files from:

```text
BepInEx/plugins/Bardheim/songs/
```

The folder is created automatically when the mod loads. Add `.mid` files there,
equip a Bardheim instrument, toggle play mode with `LeftAlt + P`, then use the
song controls to select and play them.

Lyre playback handles melodic MIDI, drum playback uses General MIDI channel 10
percussion where available, and flute playback folds melodic notes into the
current flute range.

## Multiplayer

Multiplayer audio is experimental, both-sides-required, and disabled by default.
Enable `Multiplayer.EnableMultiplayerAudio` only if every participating player
has Bardheim installed and accepts that two-client validation is deferred to a
later release.

Unmodded players are not expected to hear Bardheim instruments.

## Release Scope

This release does not add buffs, skills, server-authoritative music state,
synchronized song transfer, Harmony patches, or custom Unity asset bundles.

Source, issue context, and release notes:

https://github.com/ephraim1234-lgtm/Bardheim

Contact:

longhouselistings@gmail.com
