from __future__ import annotations

import math
import random
import wave
from pathlib import Path


SAMPLE_RATE = 44100
DURATION_SECONDS = 0.9
NOTES = [
    ("D3", 146.83),
    ("E3", 164.81),
    ("F3", 174.61),
    ("G3", 196.00),
    ("A3", 220.00),
    ("Bb3", 233.08),
    ("C4", 261.63),
    ("D4", 293.66),
    ("E4", 329.63),
    ("F4", 349.23),
    ("G4", 392.00),
]


def envelope(t: float) -> float:
    attack = min(1.0, t / 0.004)
    decay = math.exp(-3.8 * t)
    release = max(0.0, min(1.0, (DURATION_SECONDS - t) / 0.12))
    return attack * decay * release


def synthesize_pluck(frequency: float, seed: int) -> list[float]:
    # Karplus-Strong style single-string pluck. This avoids the obvious
    # octave/fifth chord impression that the first v0.2 draft had.
    rng = random.Random(seed)
    frame_count = int(SAMPLE_RATE * DURATION_SECONDS)
    period = max(2, round(SAMPLE_RATE / frequency))
    ring = [rng.uniform(-1.0, 1.0) for _ in range(period)]
    values: list[float] = []
    index = 0
    previous = 0.0

    for sample_index in range(frame_count):
        current = ring[index]
        next_index = (index + 1) % period
        damping = 0.996 - min(0.004, frequency / 320000.0)
        averaged = 0.5 * (current + ring[next_index]) * damping
        ring[index] = averaged
        index = next_index

        t = sample_index / SAMPLE_RATE
        # Light body resonance without adding another perceived pitch.
        body = math.sin(2.0 * math.pi * frequency * t) * 0.08 * math.exp(-7.5 * t)
        filtered = (current * 0.82) + (previous * 0.18)
        previous = current
        values.append((filtered + body) * envelope(t))

    return values


def write_note(path: Path, frequency: float) -> None:
    values = synthesize_pluck(frequency, seed=round(frequency * 100))
    peak = max(abs(value) for value in values)
    gain = 0.72 / peak if peak > 0 else 1.0

    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)

        for value in values:
            scaled = max(-1.0, min(1.0, value * gain))
            sample = int(scaled * 32767.0)
            wav.writeframesraw(sample.to_bytes(2, "little", signed=True))


def main() -> None:
    out_dir = Path(__file__).parent / "notes"
    out_dir.mkdir(parents=True, exist_ok=True)

    for note, frequency in NOTES:
        write_note(out_dir / f"{note}.wav", frequency)


if __name__ == "__main__":
    main()
