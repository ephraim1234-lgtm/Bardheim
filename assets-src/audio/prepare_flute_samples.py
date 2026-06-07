#!/usr/bin/env python3
"""Prepare flute note samples from Philharmonia flute MP3 source samples.

The earlier project-owner ``flute-samples`` workflow is superseded for the
embedded flute bank because phrase-derived note slices lost too much flute
character under repeated trimming. This script expects the Philharmonia all
samples flute directory, selects direct ``05_mezzo-forte_normal`` files, decodes
them through ffmpeg, and writes the project WAV bank.
"""

from __future__ import annotations

import argparse
import csv
import math
import os
import shutil
import struct
import subprocess
import wave
from dataclasses import dataclass
from pathlib import Path

import numpy as np

SAMPLE_RATE = 44100
TARGET_DURATION_SECONDS = 0.50
TARGET_PEAK = 0.68
PRE_ROLL_SECONDS = 0.0
PHILHARMONIA_FLUTE_DIR_ENV = "PHILHARMONIA_FLUTE_DIR"
FFMPEG_PATH_ENV = "FFMPEG_PATH"
SOURCE_DURATION_CODE = "05"
SOURCE_DYNAMIC = "mezzo-forte"
SOURCE_ARTICULATION = "normal"
SOURCE_DURATION_ORDER = ["05", "025", "1", "15"]
SOURCE_DYNAMIC_ORDER = ["mezzo-forte", "mezzo-piano", "piano", "pianissimo", "forte"]

DEFAULT_PHILHARMONIA_FLUTE_DIR = Path.home() / "Downloads" / "all-samples" / "all-samples" / "flute"
KNOWN_FFMPEG_PATHS = [
    Path(r"C:\Program Files (x86)\Steam\steamapps\common\RISK Global Domination\RISK_Data\StreamingAssets\BetaHub\Windows\ffmpeg.exe"),
    Path(r"C:\Program Files\BlueStacks_nxt\ffmpeg.exe"),
]

TARGET_NOTES: list[tuple[str, int]] = [
    ("D4", 62),
    ("D#4", 63),
    ("E4", 64),
    ("F4", 65),
    ("F#4", 66),
    ("G4", 67),
    ("G#4", 68),
    ("A4", 69),
    ("Bb4", 70),
    ("B4", 71),
    ("C5", 72),
    ("C#5", 73),
    ("D5", 74),
    ("D#5", 75),
    ("E5", 76),
    ("F5", 77),
    ("F#5", 78),
    ("G5", 79),
    ("G#5", 80),
    ("A5", 81),
    ("Bb5", 82),
    ("B5", 83),
]


@dataclass(frozen=True)
class PreparedSample:
    target_name: str
    target_midi: int
    source_note: str
    source_file: Path
    source_start_seconds: float
    source_end_seconds: float
    samples: np.ndarray
    segment_median_cents: float
    segment_drift_cents: float
    secondary_pulse_ratio: float
    pre_note_ratio: float
    body_sustain_ratio: float


def main() -> int:
    parser = argparse.ArgumentParser(description="Prepare Bardheim flute WAVs from Philharmonia MP3 samples.")
    parser.add_argument(
        "--source-dir",
        type=Path,
        default=Path(os.environ.get(PHILHARMONIA_FLUTE_DIR_ENV, DEFAULT_PHILHARMONIA_FLUTE_DIR)),
        help=f"Directory containing Philharmonia flute MP3s. Defaults to %{PHILHARMONIA_FLUTE_DIR_ENV}% or {DEFAULT_PHILHARMONIA_FLUTE_DIR}.",
    )
    parser.add_argument(
        "--ffmpeg",
        type=Path,
        default=None,
        help=f"Path to ffmpeg. Defaults to %{FFMPEG_PATH_ENV}%, PATH, or known local installs.",
    )
    args = parser.parse_args()

    audio_dir = Path(__file__).resolve().parent
    output_dir = audio_dir / "flute"
    manifest_path = output_dir / "flute_sample_manifest.csv"
    source_dir = args.source_dir
    ffmpeg_path = resolve_ffmpeg(args.ffmpeg)

    if not source_dir.exists():
        raise FileNotFoundError(
            f"Philharmonia flute source directory not found: {source_dir}. "
            f"Set {PHILHARMONIA_FLUTE_DIR_ENV} or pass --source-dir."
        )

    prepared = [
        prepare_note(source_dir, ffmpeg_path, target_name, target_midi)
        for target_name, target_midi in TARGET_NOTES
    ]

    output_dir.mkdir(parents=True, exist_ok=True)
    for old_wav in output_dir.glob("*.wav"):
        old_wav.unlink()

    with manifest_path.open("w", newline="", encoding="utf-8") as manifest_file:
        writer = csv.writer(manifest_file)
        writer.writerow(
            [
                "target_note",
                "target_midi",
                "source_note",
                "source_frequency_hz",
                "source_file",
                "source_start_seconds",
                "source_end_seconds",
                "retune_ratio",
                "segment_median_cents",
                "segment_drift_cents",
                "secondary_pulse_ratio",
                "pre_note_ratio",
                "body_sustain_ratio",
            ]
        )

        for sample in prepared:
            target_frequency = midi_to_frequency(sample.target_midi)
            write_wav(output_dir / f"{sample.target_name}.wav", sample.samples)
            writer.writerow(
                [
                    sample.target_name,
                    sample.target_midi,
                    sample.source_note,
                    f"{target_frequency:.3f}",
                    sample.source_file.name,
                    f"{sample.source_start_seconds:.3f}",
                    f"{sample.source_end_seconds:.3f}",
                    "1.000000",
                    f"{sample.segment_median_cents:.3f}",
                    f"{sample.segment_drift_cents:.3f}",
                    f"{sample.secondary_pulse_ratio:.3f}",
                    f"{sample.pre_note_ratio:.3f}",
                    f"{sample.body_sustain_ratio:.3f}",
                ]
            )
            print(
                f"{sample.target_name:3} <= {sample.source_file.name} "
                f"segment_median_cents={sample.segment_median_cents:+0.1f} "
                f"segment_drift_cents={sample.segment_drift_cents:+0.1f} "
                f"secondary_pulse_ratio={sample.secondary_pulse_ratio:0.2f} "
                f"pre_note_ratio={sample.pre_note_ratio:0.2f} "
                f"body_sustain_ratio={sample.body_sustain_ratio:0.2f}"
            )

    return 0


def resolve_ffmpeg(argument_path: Path | None) -> Path:
    candidates: list[Path] = []
    if argument_path is not None:
        candidates.append(argument_path)
    env_value = os.environ.get(FFMPEG_PATH_ENV)
    if env_value:
        candidates.append(Path(env_value))
    path_value = shutil.which("ffmpeg")
    if path_value:
        candidates.append(Path(path_value))
    candidates.extend(KNOWN_FFMPEG_PATHS)

    for candidate in candidates:
        if candidate.exists():
            return candidate

    raise FileNotFoundError(
        "Could not find ffmpeg for MP3 decoding. Install ffmpeg, put it on PATH, "
        f"set {FFMPEG_PATH_ENV}, or pass --ffmpeg."
    )


def prepare_note(source_dir: Path, ffmpeg_path: Path, target_name: str, target_midi: int) -> PreparedSample:
    source_note = to_philharmonia_note(target_name)
    candidates = sorted(source_dir.glob(f"flute_{source_note}_*_{SOURCE_ARTICULATION}.mp3"))
    rendered = []
    target_frequency = midi_to_frequency(target_midi)

    for source_file in candidates:
        source_duration, source_dynamic = parse_normal_source_name(source_file.name, source_note)
        if source_duration not in SOURCE_DURATION_ORDER or source_dynamic not in SOURCE_DYNAMIC_ORDER:
            continue

        decoded = decode_mp3_mono(ffmpeg_path, source_file)
        shaped, source_start_seconds, source_end_seconds = extract_one_shot(decoded)
        segment_median_cents, segment_min_cents, segment_max_cents, segment_stdev_cents, segment_drift_cents = estimate_rendered_pitch_segments(shaped, target_frequency)
        secondary_pulse_ratio, pre_note_ratio = estimate_secondary_pulse_ratio(shaped)
        body_sustain_ratio = estimate_flute_body_ratio(shaped)
        rendered.append(
            (
                score_candidate(
                    source_duration,
                    source_dynamic,
                    segment_median_cents,
                    segment_min_cents,
                    segment_max_cents,
                    segment_stdev_cents,
                    segment_drift_cents,
                    secondary_pulse_ratio,
                    body_sustain_ratio,
                ),
                PreparedSample(
                    target_name=target_name,
                    target_midi=target_midi,
                    source_note=source_note,
                    source_file=source_file,
                    source_start_seconds=source_start_seconds,
                    source_end_seconds=source_end_seconds,
                    samples=shaped,
                    segment_median_cents=segment_median_cents,
                    segment_drift_cents=segment_drift_cents,
                    secondary_pulse_ratio=secondary_pulse_ratio,
                    pre_note_ratio=pre_note_ratio,
                    body_sustain_ratio=body_sustain_ratio,
                ),
            )
        )

    if not rendered:
        raise FileNotFoundError(f"No usable Philharmonia normal flute source found for {target_name} in {source_dir}")

    return min(rendered, key=lambda item: item[0])[1]


def parse_normal_source_name(file_name: str, source_note: str) -> tuple[str, str]:
    prefix = f"flute_{source_note}_"
    suffix = f"_{SOURCE_ARTICULATION}.mp3"
    if not file_name.startswith(prefix) or not file_name.endswith(suffix):
        return "", ""

    middle = file_name[len(prefix) : -len(suffix)]
    parts = middle.split("_")
    if len(parts) != 2:
        return "", ""

    return parts[0], parts[1]


def score_candidate(
    source_duration: str,
    source_dynamic: str,
    segment_median_cents: float,
    segment_min_cents: float,
    segment_max_cents: float,
    segment_stdev_cents: float,
    segment_drift_cents: float,
    secondary_pulse_ratio: float,
    body_sustain_ratio: float,
) -> tuple[float, float, float, float, int, int]:
    range_cents = max(abs(segment_min_cents), abs(segment_max_cents))
    stability_penalty = 0.0
    stability_penalty += max(0.0, abs(segment_median_cents) - 8.0) * 6.0
    stability_penalty += max(0.0, range_cents - 18.0) * 4.0
    stability_penalty += max(0.0, segment_stdev_cents - 8.0) * 4.0
    stability_penalty += max(0.0, abs(segment_drift_cents) - 18.0) * 4.0
    stability_penalty += max(0.0, 0.65 - body_sustain_ratio) * 8.0

    duration_preference = SOURCE_DURATION_ORDER.index(source_duration)
    dynamic_preference = SOURCE_DYNAMIC_ORDER.index(source_dynamic)
    return (
        stability_penalty,
        abs(segment_median_cents),
        segment_stdev_cents,
        abs(segment_drift_cents) + max(0.0, secondary_pulse_ratio - 2.4),
        duration_preference,
        dynamic_preference,
    )


def decode_mp3_mono(ffmpeg_path: Path, source_file: Path) -> np.ndarray:
    process = subprocess.run(
        [
            str(ffmpeg_path),
            "-v",
            "error",
            "-i",
            str(source_file),
            "-f",
            "f32le",
            "-ac",
            "1",
            "-ar",
            str(SAMPLE_RATE),
            "pipe:1",
        ],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if process.returncode != 0:
        raise RuntimeError(
            f"ffmpeg could not decode {source_file}: "
            f"{process.stderr.decode('utf-8', errors='replace').strip()}"
        )
    if not process.stdout:
        raise RuntimeError(f"ffmpeg decoded no audio from {source_file}.")

    return np.frombuffer(process.stdout, dtype="<f4").astype(np.float32)


def extract_one_shot(samples: np.ndarray) -> tuple[np.ndarray, float, float]:
    if samples.size == 0:
        raise ValueError("Cannot prepare an empty sample.")

    threshold = max(0.002, float(np.max(np.abs(samples))) * 0.012)
    audible = np.flatnonzero(np.abs(samples) >= threshold)
    first_audible = int(audible[0]) if audible.size else 0
    source_start = max(0, first_audible - int(0.006 * SAMPLE_RATE))
    target_count = int(TARGET_DURATION_SECONDS * SAMPLE_RATE)
    source_end = min(samples.size, source_start + target_count)
    extracted = samples[source_start:source_end].copy()
    if extracted.size < target_count:
        extracted = np.pad(extracted, (0, target_count - extracted.size))

    return normalize(apply_edge_fades(remove_dc(extracted))), source_start / SAMPLE_RATE, source_end / SAMPLE_RATE


def apply_edge_fades(samples: np.ndarray) -> np.ndarray:
    result = samples.copy()
    fade_in = min(result.size, int(0.006 * SAMPLE_RATE))
    fade_out = min(result.size, int(0.150 * SAMPLE_RATE))

    if fade_in > 0:
        result[:fade_in] *= np.linspace(0.0, 1.0, fade_in, dtype=np.float32)
    if fade_out > 0:
        result[-fade_out:] *= np.linspace(1.0, 0.0, fade_out, dtype=np.float32)

    return result


def normalize(samples: np.ndarray) -> np.ndarray:
    peak = float(np.max(np.abs(samples))) if samples.size else 0.0
    if peak <= 0.0:
        return samples
    return samples * (TARGET_PEAK / peak)


def remove_dc(samples: np.ndarray) -> np.ndarray:
    if samples.size == 0:
        return samples
    return samples - float(np.mean(samples))


def midi_to_frequency(midi_note: int) -> float:
    return 440.0 * (2.0 ** ((midi_note - 69) / 12.0))


def to_philharmonia_note(note_name: str) -> str:
    return note_name.replace("#", "s").replace("Bb", "As")


def cents_between(actual: float, expected: float) -> float:
    if actual <= 0.0 or expected <= 0.0:
        return 9999.0
    return 1200.0 * math.log2(actual / expected)


def estimate_rendered_pitch_precise(samples: np.ndarray, expected_frequency: float) -> tuple[float, float]:
    if samples.size < 1024:
        return 0.0, 0.0

    window = samples - float(np.mean(samples))
    peak = float(np.max(np.abs(window)))
    if peak < 0.0001:
        return 0.0, 0.0

    window = window * np.hanning(window.size)
    min_lag = max(2, int(SAMPLE_RATE / (expected_frequency * 1.14)))
    max_lag = min(window.size - 3, int(SAMPLE_RATE / (expected_frequency * 0.86)))
    if max_lag <= min_lag:
        return 0.0, 0.0

    energy = float(np.dot(window, window)) + 1e-12
    best_lag = min_lag
    best_score = -1.0
    for lag in range(min_lag, max_lag + 1):
        score = float(np.dot(window[:-lag], window[lag:])) / energy
        if score > best_score:
            best_score = score
            best_lag = lag

    lag = float(best_lag)
    if 1 <= best_lag < window.size - 1:
        y0 = normalized_lag_score(window, best_lag - 1, energy)
        y1 = best_score
        y2 = normalized_lag_score(window, best_lag + 1, energy)
        denominator = y0 - (2.0 * y1) + y2
        if abs(denominator) > 1e-12:
            lag += 0.5 * (y0 - y2) / denominator

    return SAMPLE_RATE / lag, best_score


def normalized_lag_score(samples: np.ndarray, lag: int, energy: float) -> float:
    return float(np.dot(samples[:-lag], samples[lag:])) / energy


def estimate_rendered_pitch_segments(samples: np.ndarray, expected_frequency: float) -> tuple[float, float, float, float, float]:
    segment_cents: list[float] = []
    window_size = int(0.12 * SAMPLE_RATE)
    hop_size = int(0.04 * SAMPLE_RATE)
    start = int(0.04 * SAMPLE_RATE)
    end = min(samples.size - window_size, int(0.44 * SAMPLE_RATE))

    for offset in range(start, end + 1, hop_size):
        frequency, score = estimate_rendered_pitch_precise(samples[offset : offset + window_size], expected_frequency)
        if frequency > 0.0 and score > 0.08:
            segment_cents.append(cents_between(frequency, expected_frequency))

    if not segment_cents:
        return 9999.0, 9999.0, 9999.0, 9999.0, 9999.0

    values = np.array(segment_cents, dtype=np.float32)
    return (
        float(np.median(values)),
        float(np.min(values)),
        float(np.max(values)),
        float(np.std(values)),
        float(values[-1] - values[0]),
    )


def estimate_secondary_pulse_ratio(samples: np.ndarray) -> tuple[float, float]:
    first_attack_peak = max_rms(samples, 0.030, 0.140)
    if first_attack_peak <= 0.0:
        return 9999.0, 9999.0

    pre_note_peak = max_rms(samples, 0.000, 0.030)
    late_peak = max_rms(samples, 0.160, 0.400)
    return late_peak / first_attack_peak, pre_note_peak / first_attack_peak


def estimate_flute_body_ratio(samples: np.ndarray) -> float:
    attack_rms = window_rms(samples, 0.030, 0.140)
    if attack_rms <= 0.0:
        return 0.0

    body_rms = window_rms(samples, 0.160, 0.360)
    return body_rms / attack_rms


def window_rms(samples: np.ndarray, start_seconds: float, end_seconds: float) -> float:
    start = int(start_seconds * SAMPLE_RATE)
    end = min(samples.size, int(end_seconds * SAMPLE_RATE))
    if end <= start:
        return 0.0

    return float(np.sqrt(np.mean(samples[start:end] ** 2)))


def max_rms(samples: np.ndarray, start_seconds: float, end_seconds: float) -> float:
    frame_size = int(0.008 * SAMPLE_RATE)
    hop_size = int(0.002 * SAMPLE_RATE)
    start = int(start_seconds * SAMPLE_RATE)
    end = min(samples.size - frame_size, int(end_seconds * SAMPLE_RATE))
    if end < start:
        return 0.0

    peak = 0.0
    for offset in range(start, end + 1, hop_size):
        value = float(np.sqrt(np.mean(samples[offset : offset + frame_size] ** 2)))
        peak = max(peak, value)

    return peak


def write_wav(path: Path, samples: np.ndarray) -> None:
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        frames = bytearray()
        for sample in samples:
            value = max(-1.0, min(1.0, float(sample)))
            frames.extend(struct.pack("<h", int(value * 32767.0)))
        wav.writeframes(bytes(frames))


if __name__ == "__main__":
    raise SystemExit(main())
