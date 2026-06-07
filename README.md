# Bardheim

Bardheim is a Valheim mod for playable instruments. Craft a
lyre, drum, or flute, equip it, toggle play mode, press `1-8`, or play user-added MIDI
songs from the local plugin folder. The current multiplayer path is
experimental, disabled by default, and deferred for a later two-client
validation release.

## Status

- MVP scaffold: implemented.
- Automated build: passing.
- Unit tests: passing.
- Manual in-game verification: accepted for the current lyre, single-player
  drum, single-player flute, post-rename smoke, and Ctrl+F3 HUD-overlay
  behavior for the `0.7.0` package decision.
- v0.2 sample-backed local audio: accepted as the current audio baseline.
- Current held/world visual: v0.6 Hammer-carrier visual probe. The item still
  clones vanilla `Hammer`, then replaces the cloned visible renderer surface
  once at registration with a lightweight generated lyre mesh.
- MIDI song playback: implemented and manually accepted for local testing.
- Drums: implemented at build level, with a one-handed frame-drum icon/mesh
  visual probe installed for manual in-game validation.
- Flute: implemented with a separate craftable Hammer-carrier item, generated
  icon and held mesh, prepared/audited flute WAV bank, manual `1-8` melodic
  notes, chromatic `D4-B5` flute MIDI mapping, flute-specific MIDI note-rate
  limiting, and instrument-aware multiplayer routing.
- Multiplayer audio: implemented at build level for lyre, drums, and flute,
  disabled by default in `0.7.0`, pending two-client manual validation in a
  later release.
- Release readiness: `0.7.0` Thunderstore package metadata is staged under
  `package/thunderstore/`.

## Scope

Included in Milestone 1:

- One Jotunn-registered craftable lyre item cloned from vanilla `Hammer`, with
  a registration-time lightweight lyre visual replacement.
- One Jotunn-registered craftable drum item cloned from vanilla
  `Hammer` as a one-handed visual/pose carrier. The current drum visual pass
  replaces the carrier renderer with a lightweight generated frame hand-drum
  mesh and generated inventory icon; this is a manual validation probe, not
  accepted final art yet.
- One Jotunn-registered craftable flute item cloned from vanilla `Hammer` as a
  one-handed held-only carrier, with generated icon and held mesh.
- BepInEx config for play-mode toggle, local note volume, and overlap limit.
- Play mode gated by a supported instrument being equipped.
- `1-8` local routing through Jotunn input buttons with input blocking only
  while play mode is active:
  - Lyre maps `1-8` to the active tuning notes and `9` cycles tuning.
  - Drum maps manual `1-8` to `kick`, `snare`, `rim`, `clap`, `muted`,
    `open`, `low_tom`, and `high_tom`.
  - Flute maps manual `1-8` to `D4`, `E4`, `F4`, `G4`, `A4`, `Bb4`,
    `C5`, and `D5`.
- MIDI song playback while play mode is active:
  - `O`: play/stop the selected `.mid` song.
  - `[`: previous song.
  - `]`: next song.
  - `-`: decrease song speed by `0.25x`.
  - `=`: increase song speed by `0.25x`.
  - `0`: reset song speed to `1.00x`.
  - `L`: toggle looping for the selected/playing song.
  - `Ctrl + F3`: hide/show the Valheim HUD and Bardheim song overlay.
  - The in-game song list overlay shows loaded songs, controls, speed,
    loop state, progress, and the selected/playing song while lyre play mode is
    active.
  - Multiple non-percussion MIDI channels can play at the same time on lyre.
  - Drums use expanded General MIDI channel 10 percussion mapping into the
    16-hit drum bank.
  - Flute folds melodic MIDI notes into the chromatic `D4-B5` flute bank and
    mutes percussion/channel 10 by default. Default flute MIDI follows the
    lyre-style full-channel behavior, then applies
    `Audio.MaxFluteMidiNotesPerSecond` with melody-priority selection so dense
    songs do not drown out the sustained flute sound.
- Generated placeholder sine tones played locally through Unity audio.
- v0.2 embedded plucked-note WAV samples with generated-tone fallback, expanded
  to the chromatic `D3-D5` source bank for MIDI playback.
- Embedded drum hit WAV samples audited by
  `assets-src/audio/ensure_drum_hit_samples.py`. A raw-clip slicer now exports
  audition candidates from `sounds/drums`, including a selected 16-cut bank for
  listening review before promotion. The drum MIDI bank contains 16 hits for
  better General MIDI percussion coverage.
- Embedded prepared flute WAV samples under `assets-src/audio/flute/`, prepared
  by `assets-src/audio/prepare_flute_samples.py` and audited by
  `assets-src/audio/ensure_flute_note_samples.py`.
- Generated lightweight lyre held/world visual carried by the cloned `Hammer`
  item behavior.
- Generated lightweight drum icon and Hammer-carrier one-handed deep hide-drum
  mesh.
  With `VisualDebug.EnablePlayPoseCalibration = true`, drum play mode supports
  local pose tuning: numpad `4`/`6`, `2`/`8`, and `7`/`9` move X/Y/Z; hold
  `LeftShift` with those numpad keys to rotate yaw/pitch/roll. The last drum
  pose is saved to `BepInEx/config/Bardheim/drum-hand-visual-pose.txt`.
- Unit-tested note mapping and play-mode state transitions.
- Both-sides-required multiplayer audio for nearby modded players:
  - Manual `1-8` lyre notes, manual drum hits, and MIDI scheduled events emit
    transient instrument events. Flute manual notes and flute MIDI events use
    the same instrument-aware transient path at build level.
  - Remote clients validate versioned payloads, drop self-originated events,
    apply rate/overlap limits, and play accepted notes or hits spatially from
    the performer position.
  - No song files, song names, raw audio, save data, world objects, or ZDO
    state are synchronized.

Not included:

- Buffs, skills, status effects, or gameplay stat effects.
- Harmony patches.
- Custom visual asset bundles, standalone custom item paths, custom animation,
  Unity gameplay assets copied from Valheim, or direct upload.
- Server-authoritative music state, synchronized song file transfer, remote song
  UI, or support for unmodded clients hearing instrument audio.

## Build

The project defaults to the local `valheimbot` r2modman profile:

```powershell
dotnet build src/Bardheim/Bardheim.csproj `
  /p:RestoreSources=https://api.nuget.org/v3/index.json
```

To override local paths, set:

```powershell
$env:VALHEIM_INSTALL = "C:\Program Files (x86)\Steam\steamapps\common\Valheim"
$env:BEPINEX_PROFILE_DIR = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\valheimbot"
```

## Test

```powershell
dotnet test tests/Bardheim.Tests/Bardheim.Tests.csproj `
  /p:RestoreSources=https://api.nuget.org/v3/index.json
```

## Manual Verification

Local lyre, single-player drum, single-player flute, post-rename smoke, and
Ctrl+F3 HUD-overlay behavior are accepted for the `0.7.0` package decision.
Two-client multiplayer manual verification is deferred.

## MIDI Songs

User MIDI files are loaded from:

```text
BepInEx/plugins/Bardheim/songs/
```

The plugin creates this folder when needed. Add `.mid` files there, equip the
lyre, drum, or flute, toggle play mode with `LeftAlt + P`, then use `O`, `[`,
and `]`.
The song list refreshes whenever those controls are pressed, so new files can
be added without rebuilding the mod.
Pressing Valheim's `Ctrl + F3` HUD toggle also hides or shows the Bardheim song
overlay.

Supported files are Standard MIDI format 0 or 1. For lyre playback, channel 10
percussion is muted by default. For flute playback, channel 10 percussion is
also muted by default and melodic notes are folded into the chromatic `D4-B5`
flute bank. For drum playback, channel 10 percussion maps to the 16-hit drum bank,
including common fallback mappings for extended GM percussion notes. If a MIDI
file has no mapped channel-10 percussion, drum
playback falls back to pitch-band mapping from melodic notes so melody-only
files still produce drum hits. Optional sidecar profiles can be placed next to
a MIDI file as `SongName.mid.lyre-profile.json` for lyre or
`SongName.mid.flute-profile.json` for flute to choose active/muted channels,
per-channel octave offsets, arrangement mode, and flute melody density.
Instrument-specific profiles are not shared across instruments.

Dense MIDI arrangements use `Audio.MaxSimultaneousMidiNotes`, default `24`, so
multi-channel chords are not limited by the manual `1-8` note overlap setting.
Flute MIDI also uses `Audio.MaxFluteMidiNotesPerSecond`, default `14`, to keep
dense full-band songs from overwhelming the flute sample bank while preserving
lyre-style channel behavior. When the cap is hit, the engine plans each song
second ahead of playback and keeps the higher melody-carrying notes before
lower accompaniment notes.
Speed can be adjusted in-game from `0.25x` through `3.00x`.
Looping can be toggled in-game with `L`.

## Multiplayer Audio

Multiplayer audio is experimental and disabled by default in `0.7.0`.
It is both-sides-required: the performer and listener need the mod installed.
Unmodded players are not expected to hear lyre, drum, or flute audio.

Config entries are under the `Multiplayer` section:

- `EnableMultiplayerAudio`: enable sending and receiving experimental
  instrument audio events.
- `RemoteNoteVolume`: volume multiplier for notes or hits heard from other
  players.
- `RemoteMaxSimultaneousNotes`: remote overlap limit.
- `RemoteMaxNoteEventsPerSecond`: per-sender remote rate limit.
- `RemoteAudibleDistance`: spatial audio distance for remote notes.

The implementation uses Valheim routed RPC for transient note-start events only.
Manual two-client testing is still required before treating multiplayer audio as
accepted.

## Rename Notes

Bardheim uses Bardheim runtime IDs throughout: BepInEx plugin GUID, Jotunn
prefab names, routed RPC name, input button IDs, assembly, workspace, docs, and
user-facing mod name. Existing local song or pose files under old folders should
be moved manually into the Bardheim folders if needed.
