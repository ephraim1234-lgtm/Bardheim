# Bardheim

Bardheim adds playable instruments to Valheim: a lyre, drum, and flute with
manual notes and local MIDI song playback.

Craft an instrument, equip it, toggle play mode, and perform around the fire,
on the road, or in your longhouse.

## Features

- Craftable lyre, drum, and flute items.
- Manual instrument play with `1-8`.
- Local MIDI playback from your own `.mid` files.
- Song selection, speed control, looping, and in-game song overlay.
- Generated in-game instrument visuals and inventory icons.
- Valheim HUD toggle support: `Ctrl + F3` also hides Bardheim's song overlay.

## Requirements

- BepInExPack Valheim
- Jotunn

Thunderstore-compatible mod managers should install these automatically from
the package manifest.

## Installation

Install with a Thunderstore-compatible mod manager.

For manual installs, copy the package contents so the plugin lands at:

```text
BepInEx/plugins/Bardheim/Bardheim.dll
```

## MIDI Songs

Bardheim loads user MIDI files from:

```text
BepInEx/plugins/Bardheim/songs/
```

The folder is created automatically when the mod loads. Add `.mid` files there,
then use the in-game controls to select and play them.

Supported MIDI files are Standard MIDI format 0 or 1.

## Controls

- `LeftAlt + P`: toggle instrument play mode while holding a Bardheim
  instrument.
- `1-8`: play manual notes or drum hits.
- `9`: cycle lyre tuning.
- `O`: play or stop the selected MIDI song.
- `[` / `]`: previous or next MIDI song.
- `-` / `=`: decrease or increase song speed.
- `0`: reset song speed to `1.00x`.
- `L`: toggle song looping.
- `Ctrl + F3`: hide or show the Valheim HUD and Bardheim song overlay.

## Instrument Notes

Lyre playback supports manual notes, tunings, and multi-channel melodic MIDI.

Drum playback maps manual keys to drum hits and uses General MIDI channel 10
percussion where possible. If a song has no mapped percussion, Bardheim falls
back to pitch-band mapping so melody-only files can still drive the drum.

Flute playback maps manual keys to melodic notes and folds melodic MIDI into
the current flute sample range.

## Multiplayer

Version `0.7.0` focuses on local play.

Multiplayer audio exists as an experimental both-sides-required option and is
disabled by default. Enable `Multiplayer.EnableMultiplayerAudio` only if every
participating player has Bardheim installed and accepts that two-client
validation is deferred to a later release.

## Current Release Scope

Version `0.7.0` is the first public Bardheim package after the rename from
Lyre Instruments.

This release includes local lyre, drum, flute, and MIDI behavior. It does not
add buffs, skills, server-authoritative music state, synchronized song
transfer, Harmony patches, or custom Unity asset bundles.
