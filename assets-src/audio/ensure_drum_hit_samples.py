from __future__ import annotations

import argparse
import hashlib
import math
import random
import wave
from dataclasses import dataclass
from pathlib import Path


SAMPLE_RATE = 44100
CHANNELS = 1
SAMPLE_WIDTH_BYTES = 2
TARGET_PEAK = 0.78
MIN_PEAK = 0.55
MAX_PEAK = 0.86
MAX_TAIL_RMS_RATIO = 0.18

# The generic hit IDs intentionally stay MIDI-friendly, but the synthesis target
# is a Nordic frame drum palette: stretched hide body, wood hoop/stick attacks,
# bone/seed rattle texture, and small metal charm hits instead of kit cymbals.


@dataclass(frozen=True)
class DrumHit:
    name: str
    duration_seconds: float
    body_hz: float
    decay: float
    noise: float
    click: float
    seed: int
    wood: float = 0.25
    rattle: float = 0.0
    metal: float = 0.0


DRUM_HITS = [
    DrumHit("kick", 0.50, 74.0, 7.0, 0.12, 0.38, 101, wood=0.18),
    DrumHit("snare", 0.34, 168.0, 11.0, 0.46, 0.48, 211, wood=0.36, rattle=0.18),
    DrumHit("rim", 0.20, 520.0, 23.0, 0.20, 0.70, 307, wood=0.82),
    DrumHit("clap", 0.24, 300.0, 17.0, 0.42, 0.58, 409, wood=0.42),
    DrumHit("muted", 0.17, 210.0, 29.0, 0.20, 0.44, 503, wood=0.24),
    DrumHit("open", 0.64, 132.0, 6.8, 0.20, 0.36, 601, wood=0.16),
    DrumHit("low_tom", 0.50, 104.0, 8.2, 0.14, 0.32, 709, wood=0.16),
    DrumHit("high_tom", 0.39, 172.0, 10.8, 0.16, 0.36, 809, wood=0.20),
    DrumHit("low_kick", 0.58, 56.0, 6.0, 0.10, 0.34, 907, wood=0.14),
    DrumHit("electric_snare", 0.30, 205.0, 14.0, 0.58, 0.58, 1009, wood=0.48, rattle=0.28),
    DrumHit("pedal_hat", 0.15, 390.0, 34.0, 0.36, 0.52, 1103, wood=0.30, rattle=0.18),
    DrumHit("crash", 0.76, 230.0, 5.8, 0.42, 0.42, 1213, wood=0.22, rattle=0.42, metal=0.18),
    DrumHit("ride", 0.68, 260.0, 6.4, 0.34, 0.36, 1301, wood=0.24, rattle=0.34, metal=0.12),
    DrumHit("mid_tom", 0.44, 138.0, 9.4, 0.14, 0.34, 1409, wood=0.18),
    DrumHit("cowbell", 0.26, 620.0, 18.0, 0.12, 0.62, 1511, wood=0.34, metal=0.28),
    DrumHit("tambourine", 0.32, 480.0, 12.5, 0.48, 0.50, 1601, wood=0.22, rattle=0.48, metal=0.10),
]


def synthesize(hit: DrumHit) -> list[float]:
    frame_count = int(hit.duration_seconds * SAMPLE_RATE)
    rng = random.Random(hit.seed)
    samples: list[float] = []
    noise_state = 0.0

    for index in range(frame_count):
        time = index / SAMPLE_RATE
        body_env = math.exp(-time * hit.decay)
        hide_env = math.exp(-time * (hit.decay * 1.15))
        noise_env = math.exp(-time * (hit.decay * 1.65))
        rattle_env = math.exp(-time * (hit.decay * 0.70))
        click_env = math.exp(-time * 180.0)
        body = (
            math.sin(2.0 * math.pi * hit.body_hz * time) +
            (0.34 * math.sin(2.0 * math.pi * hit.body_hz * 1.52 * time)) +
            (0.18 * math.sin(2.0 * math.pi * hit.body_hz * 2.05 * time))
        ) * body_env

        noise_state = (noise_state * 0.62) + (rng.uniform(-1.0, 1.0) * 0.38)
        hide_noise = noise_state * noise_env * hit.noise
        wood_tick = math.sin(2.0 * math.pi * (860.0 + (hit.seed % 70)) * time) * click_env * hit.wood
        rattle = rng.uniform(-1.0, 1.0) * rattle_env * hit.rattle
        metal = (
            math.sin(2.0 * math.pi * 1180.0 * time) +
            (0.55 * math.sin(2.0 * math.pi * 1730.0 * time))
        ) * rattle_env * hit.metal

        click = rng.uniform(-1.0, 1.0) * click_env * hit.click
        samples.append((body * 0.82 * hide_env) + hide_noise + wood_tick + rattle + metal + click)

    fade_count = min(len(samples), int(0.012 * SAMPLE_RATE))
    for index in range(fade_count):
        fade = index / max(1, fade_count)
        samples[index] *= fade

    tail_count = min(len(samples), int(0.10 * SAMPLE_RATE))
    for index in range(tail_count):
        offset = len(samples) - tail_count + index
        fade = 1.0 - (index / max(1, tail_count - 1))
        samples[offset] *= fade

    return remove_dc(normalize(samples))


def normalize(samples: list[float]) -> list[float]:
    peak = max((abs(sample) for sample in samples), default=0.0)
    if peak <= 0.000001:
        return samples

    scale = TARGET_PEAK / peak
    return [sample * scale for sample in samples]


def remove_dc(samples: list[float]) -> list[float]:
    if not samples:
        return samples

    mean = sum(samples) / len(samples)
    return [sample - mean for sample in samples]


def write_wav(path: Path, samples: list[float]) -> None:
    pcm = bytearray()
    for sample in samples:
        value = max(-1.0, min(1.0, sample))
        pcm_value = int(round(value * 32767.0))
        pcm.extend(pcm_value.to_bytes(2, "little", signed=True))

    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(CHANNELS)
        wav.setsampwidth(SAMPLE_WIDTH_BYTES)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(bytes(pcm))


def read_wav(path: Path) -> tuple[int, list[float]]:
    with wave.open(str(path), "rb") as wav:
        channels = wav.getnchannels()
        sample_width = wav.getsampwidth()
        sample_rate = wav.getframerate()
        frames = wav.readframes(wav.getnframes())

    if channels != CHANNELS:
        raise ValueError(f"{path.name} must be mono.")

    if sample_width != SAMPLE_WIDTH_BYTES:
        raise ValueError(f"{path.name} must be 16-bit PCM.")

    values = [
        int.from_bytes(frames[index : index + 2], "little", signed=True) / 32768.0
        for index in range(0, len(frames), 2)
    ]
    return sample_rate, values


def peak(samples: list[float]) -> float:
    return max((abs(sample) for sample in samples), default=0.0)


def rms(samples: list[float]) -> float:
    if not samples:
        return 0.0

    return math.sqrt(sum(sample * sample for sample in samples) / len(samples))


def ensure_samples(drums_dir: Path) -> None:
    drums_dir.mkdir(parents=True, exist_ok=True)
    for hit in DRUM_HITS:
        path = drums_dir / f"{hit.name}.wav"
        write_wav(path, synthesize(hit))
        print(f"Wrote {path.name}")


def audit_samples(drums_dir: Path) -> int:
    failures = 0
    expected = {hit.name for hit in DRUM_HITS}
    actual = {path.stem for path in drums_dir.glob("*.wav")}
    missing = sorted(expected - actual)
    extra = sorted(actual - expected)

    if missing:
        print("Missing drum hits: " + ", ".join(missing))
        failures += len(missing)

    if extra:
        print("Unexpected drum hits: " + ", ".join(extra))
        failures += len(extra)

    for hit in DRUM_HITS:
        path = drums_dir / f"{hit.name}.wav"
        if not path.exists():
            continue

        sample_rate, samples = read_wav(path)
        duration = len(samples) / sample_rate
        current_peak = peak(samples)
        attack_frames = min(len(samples), int(sample_rate * 0.03))
        attack_peak = peak(samples[:attack_frames])
        tail_frames = min(len(samples), int(sample_rate * 0.08))
        tail_rms = rms(samples[-tail_frames:])
        digest = hashlib.sha256(path.read_bytes()).hexdigest()[:12]

        ok = (
            sample_rate == SAMPLE_RATE
            and 0.12 <= duration <= 0.90
            and MIN_PEAK <= current_peak <= MAX_PEAK
            and attack_peak >= current_peak * 0.35
            and tail_rms <= current_peak * MAX_TAIL_RMS_RATIO
        )
        status = "OK" if ok else "FAIL"
        print(
            f"{status} {hit.name:8s} duration={duration:.3f}s peak={current_peak:.3f} "
            f"attack={attack_peak:.3f} tail={tail_rms:.4f} sha256={digest}"
        )
        if not ok:
            failures += 1

    return 1 if failures else 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Ensure and audit Bardheim drum hit source samples.")
    parser.add_argument("--audit-only", action="store_true", help="Audit existing drum hit WAVs without creating missing files.")
    parser.add_argument(
        "--generate-procedural",
        action="store_true",
        help="Overwrite the drum hit WAVs with deterministic procedural fallback samples.",
    )
    args = parser.parse_args()

    drums_dir = Path(__file__).resolve().parent / "drums"
    if args.generate_procedural:
        ensure_samples(drums_dir)

    return audit_samples(drums_dir)


if __name__ == "__main__":
    raise SystemExit(main())
