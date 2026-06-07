#!/usr/bin/env python3
"""Ensure and audit Bardheim flute note source samples."""

from __future__ import annotations

import argparse
import hashlib
import math
import struct
import wave
from pathlib import Path

SAMPLE_RATE = 44100
SAMPLE_WIDTH_BYTES = 2
TARGET_PEAK = 0.68
MIN_PEAK = 0.45
MAX_PEAK = 0.86
MIN_DURATION_SECONDS = 0.45
MAX_DURATION_SECONDS = 0.52
MAX_TAIL_RATIO = 0.18
MAX_TUNING_CENTS = 18.0
MAX_SEGMENT_MEDIAN_CENTS = 8.0
MAX_SEGMENT_STDEV_CENTS = 8.0
MAX_PITCH_DRIFT_CENTS = 18.0
MAX_SECONDARY_PULSE_RATIO = 2.10
MIN_FLUTE_BODY_RATIO = 0.45

FLUTE_NOTE_MIDI: list[tuple[str, int]] = [
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

FLUTE_NOTES: list[tuple[str, float]] = [
    (note_name, 440.0 * (2.0 ** ((midi_note - 69) / 12.0)))
    for note_name, midi_note in FLUTE_NOTE_MIDI
]


def synthesize_flute_note(frequency_hz: float, duration_seconds: float = 0.50) -> list[float]:
    sample_count = int(SAMPLE_RATE * duration_seconds)
    samples: list[float] = []
    phase = 0.0
    phase_step = 2.0 * math.pi * frequency_hz / SAMPLE_RATE

    for index in range(sample_count):
        t = index / SAMPLE_RATE
        attack = min(1.0, t / 0.055)
        release_start = duration_seconds - 0.11
        release = 1.0 if t < release_start else max(0.0, (duration_seconds - t) / (duration_seconds - release_start))
        envelope = attack * release
        breath = deterministic_noise(index, frequency_hz) * 0.018 * envelope
        vibrato = math.sin(2.0 * math.pi * 5.4 * t) * 0.0035
        phase += phase_step * (1.0 + vibrato)

        tone = math.sin(phase)
        tone += 0.22 * math.sin(phase * 2.0 + 0.25)
        tone += 0.08 * math.sin(phase * 3.0 + 0.55)
        samples.append((tone * 0.72 + breath) * envelope)

    return normalize(remove_dc(apply_edge_fades(samples)))


def deterministic_noise(index: int, frequency_hz: float) -> float:
    seed = int(index * 1103515245 + frequency_hz * 1000) & 0x7FFFFFFF
    seed = (seed ^ (seed >> 13)) * 1274126177 & 0x7FFFFFFF
    return (seed / 0x3FFFFFFF) - 1.0


def normalize(samples: list[float]) -> list[float]:
    peak = max((abs(sample) for sample in samples), default=0.0)
    if peak <= 0.0:
        return samples
    scale = TARGET_PEAK / peak
    return [sample * scale for sample in samples]


def remove_dc(samples: list[float]) -> list[float]:
    if not samples:
        return samples
    mean = sum(samples) / len(samples)
    return [sample - mean for sample in samples]


def apply_edge_fades(samples: list[float]) -> list[float]:
    result = samples[:]
    fade_count = min(len(result), int(0.012 * SAMPLE_RATE))
    for index in range(fade_count):
        fade = index / fade_count
        result[index] *= fade
        result[-index - 1] *= fade
    return result


def write_wav(path: Path, samples: list[float]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(SAMPLE_WIDTH_BYTES)
        wav.setframerate(SAMPLE_RATE)
        frames = bytearray()
        for sample in samples:
            value = max(-1.0, min(1.0, sample))
            frames.extend(struct.pack("<h", int(value * 32767.0)))
        wav.writeframes(bytes(frames))


def read_wav(path: Path) -> tuple[int, list[float]]:
    with wave.open(str(path), "rb") as wav:
        channels = wav.getnchannels()
        sample_width = wav.getsampwidth()
        sample_rate = wav.getframerate()
        frames = wav.readframes(wav.getnframes())

    if channels != 1:
        raise ValueError(f"{path} must be mono.")
    if sample_width != SAMPLE_WIDTH_BYTES:
        raise ValueError(f"{path} must be 16-bit PCM.")

    values = [
        struct.unpack_from("<h", frames, offset)[0] / 32768.0
        for offset in range(0, len(frames), SAMPLE_WIDTH_BYTES)
    ]
    return sample_rate, values


def peak(samples: list[float]) -> float:
    return max((abs(sample) for sample in samples), default=0.0)


def rms(samples: list[float]) -> float:
    if not samples:
        return 0.0
    return math.sqrt(sum(sample * sample for sample in samples) / len(samples))


def estimate_pitch(samples: list[float], sample_rate: int, expected_frequency: float) -> float:
    start = int(0.12 * sample_rate)
    end = min(len(samples), start + int(0.32 * sample_rate))
    window = samples[start:end]
    if len(window) < 1024:
        return 0.0

    min_lag = max(2, int(sample_rate / (expected_frequency * 1.10)))
    max_lag = min(len(window) - 2, int(sample_rate / (expected_frequency * 0.90)))
    best_lag = min_lag
    best_score = -1.0
    for lag in range(min_lag, max_lag + 1):
        score = 0.0
        for index in range(0, len(window) - lag, 4):
            score += window[index] * window[index + lag]
        if score > best_score:
            best_score = score
            best_lag = lag

    return sample_rate / best_lag


def estimate_pitch_precise(samples: list[float], sample_rate: int, expected_frequency: float) -> tuple[float, float]:
    if len(samples) < 1024:
        return 0.0, 0.0

    mean = sum(samples) / len(samples)
    window = [(sample - mean) for sample in samples]
    peak_value = max((abs(sample) for sample in window), default=0.0)
    if peak_value < 0.0001:
        return 0.0, 0.0

    for index in range(len(window)):
        window[index] *= 0.5 - (0.5 * math.cos((2.0 * math.pi * index) / (len(window) - 1)))

    min_lag = max(2, int(sample_rate / (expected_frequency * 1.14)))
    max_lag = min(len(window) - 3, int(sample_rate / (expected_frequency * 0.86)))
    if max_lag <= min_lag:
        return 0.0, 0.0

    energy = sum(sample * sample for sample in window) + 1e-12
    best_lag = min_lag
    best_score = -1.0
    for lag in range(min_lag, max_lag + 1):
        score = 0.0
        for index in range(len(window) - lag):
            score += window[index] * window[index + lag]
        score /= energy
        if score > best_score:
            best_score = score
            best_lag = lag

    lag = float(best_lag)
    if 1 <= best_lag < len(window) - 1:
        y0 = normalized_lag_score(window, best_lag - 1, energy)
        y1 = best_score
        y2 = normalized_lag_score(window, best_lag + 1, energy)
        denominator = y0 - (2.0 * y1) + y2
        if abs(denominator) > 1e-12:
            lag += 0.5 * (y0 - y2) / denominator

    return sample_rate / lag, best_score


def normalized_lag_score(samples: list[float], lag: int, energy: float) -> float:
    score = 0.0
    for index in range(len(samples) - lag):
        score += samples[index] * samples[index + lag]
    return score / energy


def estimate_pitch_segments(samples: list[float], sample_rate: int, expected_frequency: float) -> tuple[float, float, float, float, float]:
    segment_cents: list[float] = []
    window_size = int(0.12 * sample_rate)
    hop_size = int(0.04 * sample_rate)
    start = int(0.04 * sample_rate)
    end = min(len(samples) - window_size, int(0.48 * sample_rate))

    for offset in range(start, end + 1, hop_size):
        frequency_hz, score = estimate_pitch_precise(samples[offset : offset + window_size], sample_rate, expected_frequency)
        if frequency_hz > 0.0 and score > 0.08:
            segment_cents.append(cents_between(frequency_hz, expected_frequency))

    if not segment_cents:
        return 9999.0, 9999.0, 9999.0, 9999.0, 9999.0

    ordered = sorted(segment_cents)
    middle = len(ordered) // 2
    if len(ordered) % 2 == 0:
        median_cents = (ordered[middle - 1] + ordered[middle]) / 2.0
    else:
        median_cents = ordered[middle]

    mean = sum(segment_cents) / len(segment_cents)
    stdev = math.sqrt(sum((value - mean) * (value - mean) for value in segment_cents) / len(segment_cents))
    return median_cents, min(segment_cents), max(segment_cents), stdev, segment_cents[-1] - segment_cents[0]


def estimate_secondary_pulse_ratio(samples: list[float], sample_rate: int) -> tuple[float, float]:
    first_attack_peak = max_rms(samples, sample_rate, 0.030, 0.140)
    if first_attack_peak <= 0.0:
        return 9999.0, 9999.0

    pre_note_peak = max_rms(samples, sample_rate, 0.000, 0.030)
    late_peak = max_rms(samples, sample_rate, 0.160, 0.400)
    return late_peak / first_attack_peak, pre_note_peak / first_attack_peak


def estimate_flute_body_ratio(samples: list[float], sample_rate: int) -> float:
    attack_rms = rms_window(samples, sample_rate, 0.030, 0.140)
    if attack_rms <= 0.0:
        return 0.0

    body_rms = rms_window(samples, sample_rate, 0.160, 0.360)
    return body_rms / attack_rms


def rms_window(samples: list[float], sample_rate: int, start_seconds: float, end_seconds: float) -> float:
    start = int(start_seconds * sample_rate)
    end = min(len(samples), int(end_seconds * sample_rate))
    if end <= start:
        return 0.0

    return rms(samples[start:end])


def max_rms(samples: list[float], sample_rate: int, start_seconds: float, end_seconds: float) -> float:
    frame_size = int(0.008 * sample_rate)
    hop_size = int(0.002 * sample_rate)
    start = int(start_seconds * sample_rate)
    end = min(len(samples) - frame_size, int(end_seconds * sample_rate))
    if end < start:
        return 0.0

    peak_value = 0.0
    for offset in range(start, end + 1, hop_size):
        peak_value = max(peak_value, rms(samples[offset : offset + frame_size]))

    return peak_value


def cents_between(actual: float, expected: float) -> float:
    if actual <= 0.0 or expected <= 0.0:
        return 9999.0
    return 1200.0 * math.log2(actual / expected)


def ensure_samples(flute_dir: Path) -> None:
    for note_name, frequency_hz in FLUTE_NOTES:
        write_wav(flute_dir / f"{note_name}.wav", synthesize_flute_note(frequency_hz))


def audit_samples(flute_dir: Path) -> int:
    failures = 0
    expected_names = {note_name for note_name, _ in FLUTE_NOTES}
    actual_names = {path.stem for path in flute_dir.glob("*.wav")}

    for missing in sorted(expected_names - actual_names):
        print(f"MISSING {missing}")
        failures += 1

    for extra in sorted(actual_names - expected_names):
        print(f"EXTRA {extra}")
        failures += 1

    for note_name, expected_frequency in FLUTE_NOTES:
        path = flute_dir / f"{note_name}.wav"
        if not path.exists():
            continue

        sample_rate, samples = read_wav(path)
        duration = len(samples) / sample_rate
        current_peak = peak(samples)
        tail_frames = min(len(samples), int(sample_rate * 0.08))
        tail_rms = rms(samples[-tail_frames:])
        estimated_frequency = estimate_pitch(samples, sample_rate, expected_frequency)
        cents = cents_between(estimated_frequency, expected_frequency)
        segment_median_cents, segment_min_cents, segment_max_cents, segment_stdev_cents, segment_drift_cents = estimate_pitch_segments(samples, sample_rate, expected_frequency)
        secondary_pulse_ratio, pre_note_ratio = estimate_secondary_pulse_ratio(samples, sample_rate)
        body_sustain_ratio = estimate_flute_body_ratio(samples, sample_rate)
        sha = hashlib.sha256(path.read_bytes()).hexdigest()[:12]
        ok = (
            sample_rate == SAMPLE_RATE
            and MIN_DURATION_SECONDS <= duration <= MAX_DURATION_SECONDS
            and MIN_PEAK <= current_peak <= MAX_PEAK
            and tail_rms <= current_peak * MAX_TAIL_RATIO
            and abs(cents) <= MAX_TUNING_CENTS
            and abs(segment_median_cents) <= MAX_SEGMENT_MEDIAN_CENTS
            and segment_stdev_cents <= MAX_SEGMENT_STDEV_CENTS
            and abs(segment_drift_cents) <= MAX_PITCH_DRIFT_CENTS
            and secondary_pulse_ratio <= MAX_SECONDARY_PULSE_RATIO
            and body_sustain_ratio >= MIN_FLUTE_BODY_RATIO
        )
        status = "OK" if ok else "FAIL"
        print(
            f"{status} {note_name:3} duration={duration:0.3f}s peak={current_peak:0.3f} "
            f"tail={tail_rms:0.4f} pitch={estimated_frequency:7.2f}Hz cents={cents:6.1f} "
            f"segment_median_cents={segment_median_cents:6.1f} segment_range={segment_min_cents:6.1f}..{segment_max_cents:6.1f} "
            f"segment_stdev_cents={segment_stdev_cents:5.1f} segment_drift_cents={segment_drift_cents:6.1f} "
            f"pre_note_ratio={pre_note_ratio:0.2f} secondary_pulse_ratio={secondary_pulse_ratio:0.2f} "
            f"body_sustain_ratio={body_sustain_ratio:0.2f} sha256={sha}"
        )
        if not ok:
            failures += 1

    return failures


def main() -> int:
    parser = argparse.ArgumentParser(description="Ensure and audit Bardheim flute note source samples.")
    parser.add_argument("--generate", action="store_true", help="Generate deterministic procedural breathy flute WAVs.")
    args = parser.parse_args()

    flute_dir = Path(__file__).resolve().parent / "flute"
    if args.generate:
        ensure_samples(flute_dir)

    return audit_samples(flute_dir)


if __name__ == "__main__":
    raise SystemExit(main())
