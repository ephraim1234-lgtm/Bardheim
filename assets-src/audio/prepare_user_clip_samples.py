from __future__ import annotations

import math
import wave
from dataclasses import dataclass
from pathlib import Path

import numpy as np


SAMPLE_RATE = 44100
CHANNELS = 1
SAMPLE_WIDTH_BYTES = 2
OUTPUT_SECONDS = 0.9
SOURCE_SECONDS = 1.15
FADE_IN_SECONDS = 0.006
FADE_OUT_SECONDS = 0.18
TARGET_PEAK = 0.78
SINGLE_PLUCK_DECAY = 6.6


@dataclass(frozen=True)
class ClipMapping:
    note: str
    source_file: str
    start_seconds: float
    source_frequency_hz: float
    target_frequency_hz: float


MAPPINGS = [
    ClipMapping(
        "D3",
        "Ancient Nordic Lyre, Simple Medieval Melody, Slow And Contemplative, Scandina.wav",
        1.82,
        146.83,
        146.83,
    ),
    ClipMapping(
        "E3",
        "Ancient Nordic Lyre, Simple Medieval Melody, Slow And Contemplative, Scandina1.wav",
        0.38,
        329.63,
        164.81,
    ),
    ClipMapping(
        "F3",
        "Primitive Scandinavian Lyre, Solitary Nordic Folk Musician, Rustic Wooden Ins.wav",
        0.45,
        174.61,
        174.61,
    ),
    ClipMapping(
        "G3",
        "Ancient Nordic Lyre, Simple Medieval Melody, Slow And Contemplative, Scandina.wav",
        0.75,
        392.00,
        196.00,
    ),
    ClipMapping(
        "A3",
        "Ancient Nordic Lyre, Simple Medieval Melody, Slow And Contemplative, Scandina1.wav",
        2.29,
        220.00,
        220.00,
    ),
    ClipMapping(
        "Bb3",
        "Ancient Nordic Lyre, Simple Medieval Melody, Slow And Contemplative, Scandina1.wav",
        2.29,
        220.00,
        233.08,
    ),
    ClipMapping(
        "C4",
        "Ancient Nordic Lyre, Simple Medieval Melody, Slow And Contemplative, Scandina1.wav",
        1.49,
        261.63,
        261.63,
    ),
    ClipMapping(
        "D4",
        "Primitive Scandinavian Lyre, Solitary Nordic Folk Musician, Rustic Wooden Ins1.wav",
        0.13,
        293.66,
        293.66,
    ),
    ClipMapping(
        "E4",
        "Ancient Nordic Lyre, Simple Medieval Melody, Slow And Contemplative, Scandina1.wav",
        0.38,
        329.63,
        329.63,
    ),
    ClipMapping(
        "F4",
        "Ancient Nordic Lyre, Simple Medieval Melody, Slow And Contemplative, Scandina.wav",
        2.81,
        349.23,
        349.23,
    ),
    ClipMapping(
        "G4",
        "Ancient Nordic Lyre, Simple Medieval Melody, Slow And Contemplative, Scandina.wav",
        0.75,
        392.00,
        392.00,
    ),
]


def main() -> None:
    script_dir = Path(__file__).resolve().parent
    repo_root = script_dir.parents[1]
    source_dir = repo_root / "sounds"
    out_dir = script_dir / "notes"
    out_dir.mkdir(parents=True, exist_ok=True)

    for mapping in MAPPINGS:
        source_path = source_dir / mapping.source_file
        source_rate, source = read_wav_mono(source_path)
        extracted = extract_window(source, source_rate, mapping.start_seconds)
        shifted = pitch_shift(extracted, mapping.source_frequency_hz, mapping.target_frequency_hz)
        resampled = resample(shifted, source_rate, SAMPLE_RATE)
        prepared = prepare_one_shot(resampled)
        write_wav(out_dir / f"{mapping.note}.wav", prepared)
        print(f"Wrote {mapping.note}.wav from {mapping.source_file} at {mapping.start_seconds:.2f}s")


def read_wav_mono(path: Path) -> tuple[int, np.ndarray]:
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


def extract_window(samples: np.ndarray, sample_rate: int, start_seconds: float) -> np.ndarray:
    preroll = int(0.015 * sample_rate)
    start = max(0, int(start_seconds * sample_rate) - preroll)
    length = int(SOURCE_SECONDS * sample_rate)
    end = min(len(samples), start + length)
    window = samples[start:end]
    if len(window) < length:
        window = np.pad(window, (0, length - len(window)))

    return trim_to_attack(window)


def trim_to_attack(samples: np.ndarray) -> np.ndarray:
    frame = 256
    hop = 64
    if len(samples) < frame:
        return samples

    rms = []
    for offset in range(0, len(samples) - frame, hop):
        chunk = samples[offset : offset + frame]
        rms.append(float(math.sqrt(float(np.mean(chunk * chunk)))))

    if not rms:
        return samples

    threshold = max(0.015, max(rms) * 0.18)
    for index, value in enumerate(rms):
        if value >= threshold:
            start = max(0, (index * hop) - 128)
            return samples[start:]

    return samples


def pitch_shift(samples: np.ndarray, source_frequency_hz: float, target_frequency_hz: float) -> np.ndarray:
    speed = target_frequency_hz / source_frequency_hz
    if abs(speed - 1.0) < 0.0001:
        return samples

    source_positions = np.arange(0, len(samples), speed, dtype=np.float32)
    source_positions = source_positions[source_positions < len(samples) - 1]
    return np.interp(source_positions, np.arange(len(samples)), samples).astype(np.float32)


def resample(samples: np.ndarray, source_rate: int, target_rate: int) -> np.ndarray:
    if source_rate == target_rate:
        return samples.astype(np.float32)

    target_count = max(1, round(len(samples) * target_rate / source_rate))
    source_positions = np.linspace(0, len(samples) - 1, target_count)
    return np.interp(source_positions, np.arange(len(samples)), samples).astype(np.float32)


def prepare_one_shot(samples: np.ndarray) -> np.ndarray:
    target_count = int(OUTPUT_SECONDS * SAMPLE_RATE)
    if len(samples) < target_count:
        samples = np.pad(samples, (0, target_count - len(samples)))
    else:
        samples = samples[:target_count]

    samples = remove_dc(samples)
    samples = apply_single_pluck_envelope(samples)
    samples = apply_fades(samples)
    peak = float(np.max(np.abs(samples)))
    if peak > 0.0001:
        samples = samples * (TARGET_PEAK / peak)

    return np.clip(samples, -1.0, 1.0).astype(np.float32)


def remove_dc(samples: np.ndarray) -> np.ndarray:
    return samples - float(np.mean(samples))


def apply_fades(samples: np.ndarray) -> np.ndarray:
    result = samples.copy()
    fade_in = min(len(result), int(FADE_IN_SECONDS * SAMPLE_RATE))
    fade_out = min(len(result), int(FADE_OUT_SECONDS * SAMPLE_RATE))

    if fade_in > 0:
        result[:fade_in] *= np.linspace(0.0, 1.0, fade_in)

    if fade_out > 0:
        result[-fade_out:] *= np.linspace(1.0, 0.0, fade_out)

    return result


def apply_single_pluck_envelope(samples: np.ndarray) -> np.ndarray:
    t = np.arange(len(samples), dtype=np.float32) / SAMPLE_RATE
    envelope = np.exp(-SINGLE_PLUCK_DECAY * t)
    return samples * envelope


def write_wav(path: Path, samples: np.ndarray) -> None:
    pcm = (samples * 32767.0).astype("<i2")
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(CHANNELS)
        wav.setsampwidth(SAMPLE_WIDTH_BYTES)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(pcm.tobytes())


if __name__ == "__main__":
    main()
