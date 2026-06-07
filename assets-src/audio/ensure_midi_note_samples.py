from __future__ import annotations

import argparse
import math
import shutil
import wave
from dataclasses import dataclass
from pathlib import Path

import numpy as np


SAMPLE_RATE = 44100
CHANNELS = 1
SAMPLE_WIDTH_BYTES = 2
TARGET_PEAK = 0.78
PITCH_TOLERANCE_CENTS = 15.0
RETUNE_THRESHOLD_CENTS = 5.0

NOTE_SEMITONES = {
    "C": 0,
    "C#": 1,
    "Db": 1,
    "D": 2,
    "D#": 3,
    "Eb": 3,
    "E": 4,
    "F": 5,
    "F#": 6,
    "Gb": 6,
    "G": 7,
    "G#": 8,
    "Ab": 8,
    "A": 9,
    "A#": 10,
    "Bb": 10,
    "B": 11,
}

MIDI_BANK_NOTES = [
    "D3",
    "D#3",
    "E3",
    "F3",
    "F#3",
    "G3",
    "G#3",
    "A3",
    "A#3",
    "B3",
    "C4",
    "C#4",
    "D4",
    "D#4",
    "E4",
    "F4",
    "F#4",
    "G4",
    "G#4",
    "A4",
    "A#4",
    "B4",
    "C5",
    "C#5",
    "D5",
]

FLAT_ALIASES = {
    "Bb3": "A#3",
    "Bb4": "A#4",
}


@dataclass(frozen=True)
class NoteInfo:
    name: str
    midi: int
    frequency_hz: float


def parse_note(name: str) -> NoteInfo:
    pitch_class = name[:-1]
    octave = int(name[-1])
    midi = (octave + 1) * 12 + NOTE_SEMITONES[pitch_class]
    frequency_hz = 440.0 * (2.0 ** ((midi - 69) / 12.0))
    return NoteInfo(name, midi, frequency_hz)


def read_wav(path: Path) -> tuple[int, np.ndarray]:
    with wave.open(str(path), "rb") as wav:
        channels = wav.getnchannels()
        sample_width = wav.getsampwidth()
        sample_rate = wav.getframerate()
        frame_count = wav.getnframes()
        frames = wav.readframes(frame_count)

    if sample_width != SAMPLE_WIDTH_BYTES:
        raise ValueError(f"{path} must be 16-bit PCM.")

    samples = np.frombuffer(frames, dtype="<i2").astype(np.float32)
    if channels > 1:
        samples = samples.reshape(-1, channels).mean(axis=1)

    return sample_rate, samples / 32768.0


def write_wav(path: Path, samples: np.ndarray) -> None:
    peak = float(np.max(np.abs(samples))) if len(samples) else 0.0
    if peak > 0.0001:
        samples = samples * (TARGET_PEAK / peak)

    pcm = (np.clip(samples, -1.0, 1.0) * 32767.0).astype("<i2")
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(CHANNELS)
        wav.setsampwidth(SAMPLE_WIDTH_BYTES)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(pcm.tobytes())


def pitch_shift(samples: np.ndarray, source_frequency_hz: float, target_frequency_hz: float) -> np.ndarray:
    speed = target_frequency_hz / source_frequency_hz
    if abs(speed - 1.0) < 0.0001:
        return samples.copy()

    source_positions = np.arange(0, len(samples), speed, dtype=np.float32)
    source_positions = source_positions[source_positions < len(samples) - 1]
    shifted = np.interp(source_positions, np.arange(len(samples)), samples).astype(np.float32)
    return apply_edge_fades(remove_dc(shifted))


def remove_dc(samples: np.ndarray) -> np.ndarray:
    return samples - float(np.mean(samples))


def apply_edge_fades(samples: np.ndarray) -> np.ndarray:
    result = samples.copy()
    fade_in = min(len(result), int(0.006 * SAMPLE_RATE))
    fade_out = min(len(result), int(0.18 * SAMPLE_RATE))

    if fade_in > 0:
        result[:fade_in] *= np.linspace(0.0, 1.0, fade_in)

    if fade_out > 0:
        result[-fade_out:] *= np.linspace(1.0, 0.0, fade_out)

    return result


def estimate_pitch_cents(path: Path, target_frequency_hz: float) -> tuple[float, float]:
    sample_rate, samples = read_wav(path)
    if sample_rate != SAMPLE_RATE:
        raise ValueError(f"{path} must use {SAMPLE_RATE} Hz.")

    peak = float(np.max(np.abs(samples))) if len(samples) else 0.0
    if peak <= 0.000001:
        raise ValueError(f"{path} is silent.")

    threshold = max(0.02, peak * 0.12)
    attack_candidates = np.flatnonzero(np.abs(samples) > threshold)
    attack = int(attack_candidates[0]) if len(attack_candidates) else 0
    start = min(len(samples) - 1, attack + int(0.015 * sample_rate))
    length = min(int(0.32 * sample_rate), len(samples) - start)
    if length < int(0.12 * sample_rate):
        start = 0
        length = min(len(samples), int(0.32 * sample_rate))

    window = samples[start : start + length].copy()
    window = remove_dc(window)
    if len(window) < 1024:
        raise ValueError(f"{path} is too short for pitch estimation.")

    window *= np.hanning(len(window))
    correlation = np.correlate(window, window, mode="full")[len(window) - 1 :]
    if correlation[0] <= 0.000001:
        raise ValueError(f"{path} has no usable pitch window.")

    correlation = correlation / correlation[0]
    candidates: list[tuple[float, float, float]] = []
    for multiplier in (0.5, 1.0, 2.0):
        expected_frequency = target_frequency_hz * multiplier
        min_lag = max(2, int(sample_rate / (expected_frequency * 1.08)))
        max_lag = min(len(correlation) - 1, int(sample_rate / (expected_frequency * 0.92)))
        if max_lag <= min_lag:
            continue

        lag = min_lag + int(np.argmax(correlation[min_lag : max_lag + 1]))
        refined_lag = refine_peak_lag(correlation, lag)
        candidates.append((float(correlation[lag]), sample_rate / refined_lag, multiplier))

    if not candidates:
        raise ValueError(f"{path} has no usable pitch candidate.")

    strongest = max(candidates, key=lambda candidate: candidate[0])
    target_octave = next((candidate for candidate in candidates if candidate[2] == 1.0), strongest)
    chosen = target_octave if target_octave[0] >= strongest[0] * 0.72 else strongest
    estimated_frequency = chosen[1]
    cents = 1200.0 * math.log2(estimated_frequency / target_frequency_hz)
    return estimated_frequency, cents


def refine_peak_lag(correlation: np.ndarray, lag: int) -> float:
    if lag <= 0 or lag >= len(correlation) - 1:
        return float(lag)

    left = float(correlation[lag - 1])
    center = float(correlation[lag])
    right = float(correlation[lag + 1])
    denominator = left - (2.0 * center) + right
    if abs(denominator) <= 0.000001:
        return float(lag)

    offset = 0.5 * (left - right) / denominator
    return float(lag) + max(-0.5, min(0.5, offset))


def nearest_source(note: NoteInfo, available: dict[str, Path]) -> tuple[NoteInfo, Path]:
    source_notes = [parse_note(path.stem) for path in available.values()]
    source_notes.sort(key=lambda source: (abs(source.midi - note.midi), source.name))
    source_note = source_notes[0]
    return source_note, available[source_note.name]


def create_missing_samples(notes_dir: Path) -> None:
    available = {path.stem: path for path in notes_dir.glob("*.wav")}

    for flat_name, sharp_name in FLAT_ALIASES.items():
        flat_path = notes_dir / f"{flat_name}.wav"
        sharp_path = notes_dir / f"{sharp_name}.wav"
        if flat_path.exists() and not sharp_path.exists():
            shutil.copyfile(flat_path, sharp_path)
            print(f"Created alias {sharp_name}.wav from {flat_name}.wav")
            available[sharp_name] = sharp_path

    for note_name in MIDI_BANK_NOTES:
        target_path = notes_dir / f"{note_name}.wav"
        if target_path.exists():
            continue

        note = parse_note(note_name)
        source_note, source_path = nearest_source(note, available)
        source_rate, source_samples = read_wav(source_path)
        if source_rate != SAMPLE_RATE:
            raise ValueError(f"{source_path} must use {SAMPLE_RATE} Hz.")

        generated = pitch_shift(source_samples, source_note.frequency_hz, note.frequency_hz)
        write_wav(target_path, generated)
        available[note_name] = target_path
        print(f"Created {note_name}.wav from {source_note.name}.wav")


def retune_outliers(notes_dir: Path) -> None:
    expected_names = sorted(set(MIDI_BANK_NOTES).union(FLAT_ALIASES.keys()), key=lambda name: parse_note(name).midi)
    for note_name in expected_names:
        note = parse_note(note_name)
        path = notes_dir / f"{note_name}.wav"
        estimated_frequency, cents = estimate_pitch_cents(path, note.frequency_hz)
        if abs(cents) <= RETUNE_THRESHOLD_CENTS:
            continue

        _, samples = read_wav(path)
        retuned = pitch_shift(samples, estimated_frequency, note.frequency_hz)
        write_wav(path, retuned)
        print(f"Retuned {note_name}.wav by {-cents:.1f} cents")


def audit_samples(notes_dir: Path) -> int:
    expected_names = sorted(set(MIDI_BANK_NOTES).union(FLAT_ALIASES.keys()), key=lambda name: parse_note(name).midi)
    missing = [name for name in expected_names if not (notes_dir / f"{name}.wav").exists()]
    if missing:
        print("Missing notes: " + ", ".join(missing))
        return 1

    failures = 0
    for note_name in expected_names:
        note = parse_note(note_name)
        path = notes_dir / f"{note_name}.wav"
        estimated_frequency, cents = estimate_pitch_cents(path, note.frequency_hz)
        status = "OK" if abs(cents) <= PITCH_TOLERANCE_CENTS else "FAIL"
        print(
            f"{status} {note_name:4s} target={note.frequency_hz:7.2f}Hz "
            f"estimate={estimated_frequency:7.2f}Hz cents={cents:6.1f}"
        )
        if status == "FAIL":
            failures += 1

    return 1 if failures else 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Ensure and audit Bardheim MIDI note source samples.")
    parser.add_argument("--create-missing", action="store_true", help="Create missing chromatic D3-D5 samples.")
    parser.add_argument(
        "--retune-outliers",
        action="store_true",
        help=f"Retune samples farther than {RETUNE_THRESHOLD_CENTS:.0f} cents from target pitch.",
    )
    args = parser.parse_args()

    notes_dir = Path(__file__).resolve().parent / "notes"
    notes_dir.mkdir(parents=True, exist_ok=True)

    if args.create_missing:
        create_missing_samples(notes_dir)

    if args.retune_outliers:
        retune_outliers(notes_dir)

    return audit_samples(notes_dir)


if __name__ == "__main__":
    raise SystemExit(main())
