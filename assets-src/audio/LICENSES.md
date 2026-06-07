# Lyre Audio Source Notes

## Provenance

The v0.2 note samples are prepared for this repository from user-supplied
source clips outside this repository using:

- `prepare_user_clip_samples.py`

The source clips were created and supplied by the project owner for this mod.
They are not Valheim, Unity, BepInEx, Jotunn, or third-party sample assets.

The previous deterministic local Karplus-Strong generator remains available as
a fallback/reference:

- `generate_lyre_samples.py`

The future MIDI/source-bank coverage and tuning check is maintained with:

- `ensure_midi_note_samples.py --create-missing`
- `ensure_midi_note_samples.py --retune-outliers`

That script creates missing chromatic samples from the nearest existing lyre
sample, creates `A#3`/`A#4` aliases from the existing `Bb3`/`Bb4` samples for
MIDI-style sharp naming, retunes samples more than 5 cents from their target
pitch, and audits every source-bank sample against a 15-cent failure threshold.

The v1 drum source bank started as deterministic procedural project assets, but
the current quality workflow is moving to curated one-shots sliced from
project-owner raw drum clips outside this repository using:

- `slice_drum_raw_clips.py`
- `drum_candidate_selection.csv`

The procedural fallback remains available only as an explicit reference path:

- `ensure_drum_hit_samples.py`

Use `ensure_drum_hit_samples.py` normally for audit-only validation. Use
`--generate-procedural` only when intentionally replacing the current drum WAVs
with deterministic fallback sounds. The current raw clips and derived candidate
cuts were created and supplied by the project owner for this mod. They are not
Valheim, Unity, BepInEx, Jotunn, or third-party sample assets.

The v1 flute source bank was initially prepared from project-owner flute
performance WAVs under `assets-src/audio/flute-samples/`. The current active
embedded flute bank is prepared from the project owner's local Philharmonia
Orchestra flute sample download outside this repository using:

using:

- `prepare_flute_samples.py`

The script selects direct-note Philharmonia `normal` flute MP3s for the
chromatic `D4-B5` runtime bank, decodes them with `ffmpeg`, writes mono 44.1
kHz WAVs under `assets-src/audio/flute/`, and records provenance for each
derived note in `assets-src/audio/flute/flute_sample_manifest.csv`.

The deterministic procedural flute generator remains available only as an
explicit fallback/reference path:

- `ensure_flute_note_samples.py --generate`

Use `ensure_flute_note_samples.py` normally for audit-only validation.

Philharmonia's published sound-sample terms allow use in music, films, video,
radio, games, apps, and commercial/non-commercial work, but restrict making the
samples available as-is or as sampler instruments. The project owner accepted
the release risk for the derived flute bank in Bardheim `0.7.0`. Do not commit
or redistribute the full raw Philharmonia `all-samples` download.

## Requirements

- One mono WAV per note used by the built-in v0.2 tunings.
- Notes: `D3`, `E3`, `F3`, `G3`, `A3`, `Bb3`, `C4`, `D4`, `E4`,
  `F4`, `G4`.
- Additional approved source-bank notes for future MIDI/song work may live in
  `assets-src/audio/notes/` without being embedded into the current runtime
  build. As of 2026-06-02, the future-use bank covers chromatic notes from
  `D3` through `D5`: `D3`, `D#3`, `E3`, `F3`, `F#3`, `G3`, `G#3`,
  `A3`, `A#3`, `B3`, `C4`, `C#4`, `D4`, `D#4`, `E4`, `F4`, `F#4`,
  `G4`, `G#4`, `A4`, `A#4`, `B4`, `C5`, `C#5`, and `D5`.
  `Bb3` and `Bb4` remain available for the current flat-named built-in
  tunings.
- Short plucked attack and natural decay.
- No third-party samples without explicit license notes.
- Avoid strong octave/fifth harmonic layers that make one note sound like a
  chord.

## License Intent

Original project assets, pending human review before any public release.
