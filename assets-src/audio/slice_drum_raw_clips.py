from __future__ import annotations

import argparse
import csv
import math
import wave
from dataclasses import dataclass
from pathlib import Path


SAMPLE_RATE = 44100
CHANNELS = 1
SAMPLE_WIDTH_BYTES = 2
TARGET_PEAK = 0.78
# Raw project-owner source clips default to ./sounds/drums at the repo root.
DEFAULT_RAW_DIR = Path(__file__).resolve().parents[1] / "sounds" / "drums"
DEFAULT_OUTPUT_DIR = Path(__file__).resolve().parent / "drum-candidates"
FINAL_DRUM_DIR = Path(__file__).resolve().parent / "drums"


@dataclass(frozen=True)
class Candidate:
    source_name: str
    index: int
    start_seconds: float
    end_seconds: float
    peak: float
    score: float
    output_name: str


def read_wav_mono(path: Path) -> tuple[int, list[float]]:
    with wave.open(str(path), "rb") as wav:
        source_rate = wav.getframerate()
        channels = wav.getnchannels()
        sample_width = wav.getsampwidth()
        frames = wav.readframes(wav.getnframes())

    if sample_width != SAMPLE_WIDTH_BYTES:
        raise ValueError(f"{path.name} must be 16-bit PCM.")

    values = [
        int.from_bytes(frames[index : index + 2], "little", signed=True) / 32768.0
        for index in range(0, len(frames), 2)
    ]

    if channels == 1:
        mono = values
    else:
        mono = []
        for index in range(0, len(values), channels):
            frame = values[index : index + channels]
            mono.append(sum(frame) / len(frame))

    if source_rate == SAMPLE_RATE:
        return SAMPLE_RATE, mono

    return SAMPLE_RATE, resample_linear(mono, source_rate, SAMPLE_RATE)


def resample_linear(samples: list[float], source_rate: int, target_rate: int) -> list[float]:
    if not samples:
        return []

    target_count = int(round(len(samples) * target_rate / source_rate))
    output: list[float] = []
    for target_index in range(target_count):
        source_position = target_index * source_rate / target_rate
        left = int(source_position)
        right = min(left + 1, len(samples) - 1)
        fraction = source_position - left
        output.append((samples[left] * (1.0 - fraction)) + (samples[right] * fraction))

    return output


def detect_transient_candidates(samples: list[float], sample_rate: int, max_candidates: int) -> list[int]:
    window = max(1, int(sample_rate * 0.018))
    hop = max(1, int(sample_rate * 0.008))
    energies: list[float] = []
    for start in range(0, max(1, len(samples) - window), hop):
        segment = samples[start : start + window]
        energies.append(rms(segment))

    if not energies:
        return []

    sorted_energies = sorted(energies)
    median = sorted_energies[len(sorted_energies) // 2]
    high_index = min(len(sorted_energies) - 1, int(len(sorted_energies) * 0.88))
    high = sorted_energies[high_index]
    threshold = max(0.018, median * 2.4, high * 0.42)
    minimum_gap = int(0.22 / (hop / sample_rate))

    scored: list[tuple[float, int]] = []
    for index in range(1, len(energies) - 1):
        energy = energies[index]
        if energy < threshold:
            continue

        previous = energies[index - 1]
        next_energy = energies[index + 1]
        if energy < previous or energy < next_energy:
            continue

        onset_score = energy - previous
        scored.append((energy + max(0.0, onset_score * 2.0), index))

    scored.sort(reverse=True)
    selected: list[int] = []
    for _, index in scored:
        if all(abs(index - existing) >= minimum_gap for existing in selected):
            selected.append(index)
        if len(selected) >= max_candidates:
            break

    selected.sort()
    return [index * hop for index in selected]


def extract_candidate(samples: list[float], onset: int, sample_rate: int, pre_roll: float, duration: float) -> tuple[float, float, list[float]]:
    start = max(0, onset - int(pre_roll * sample_rate))
    end = min(len(samples), start + int(duration * sample_rate))
    clip = samples[start:end]
    clip = postprocess_candidate(clip, sample_rate, trim_tail=True)
    return start / sample_rate, (start + len(clip)) / sample_rate, clip


def trim_quiet_tail(samples: list[float], sample_rate: int) -> list[float]:
    if not samples:
        return samples

    window = max(1, int(sample_rate * 0.015))
    current_peak = peak(samples)
    threshold = max(0.012, current_peak * 0.055)
    end = len(samples)
    for index in range(len(samples) - window, 0, -window):
        if peak(samples[index : index + window]) > threshold:
            end = min(len(samples), index + int(sample_rate * 0.055))
            break

    minimum = min(len(samples), int(sample_rate * 0.12))
    return samples[: max(minimum, end)]


def apply_fades(samples: list[float], sample_rate: int) -> list[float]:
    if not samples:
        return samples

    output = list(samples)
    fade_in = min(len(output), int(sample_rate * 0.002))
    for index in range(fade_in):
        output[index] *= index / max(1, fade_in)

    fade_out = min(len(output), int(sample_rate * 0.08))
    for index in range(fade_out):
        offset = len(output) - fade_out + index
        output[offset] *= 1.0 - (index / max(1, fade_out - 1))

    return output


def pad_min_duration(samples: list[float], sample_rate: int, minimum_seconds: float) -> list[float]:
    minimum_frames = int(sample_rate * minimum_seconds)
    if len(samples) >= minimum_frames:
        return samples

    return samples + ([0.0] * (minimum_frames - len(samples)))


def dampen_tail(samples: list[float], sample_rate: int, start_seconds: float, decay: float) -> list[float]:
    output = list(samples)
    start = int(sample_rate * start_seconds)
    for index in range(start, len(output)):
        time = (index - start) / sample_rate
        output[index] *= math.exp(-time * decay)

    return output


def soft_tail_gate(samples: list[float], sample_rate: int, start_seconds: float, floor_gain: float) -> list[float]:
    output = list(samples)
    preserve = int(sample_rate * start_seconds)
    if preserve >= len(output):
        return output

    window = max(1, int(sample_rate * 0.010))
    attack_reference = rms(output[: min(len(output), int(sample_rate * 0.035))])
    threshold = max(0.018, attack_reference * 0.20)
    floor_gain = max(0.0, min(1.0, floor_gain))

    for start in range(preserve, len(output), window):
        end = min(len(output), start + window)
        segment_rms = rms(output[start:end])
        if segment_rms >= threshold:
            continue

        gain = floor_gain + ((1.0 - floor_gain) * (segment_rms / threshold))
        for index in range(start, end):
            output[index] *= gain

    return output


def postprocess_candidate(
    samples: list[float],
    sample_rate: int,
    tail_start: float = 0.035,
    tail_decay: float = 14.0,
    gate_floor: float = 0.16,
    trim_tail: bool = False,
) -> list[float]:
    clip = remove_dc(samples)
    clip = dampen_tail(clip, sample_rate, tail_start, tail_decay)
    clip = soft_tail_gate(clip, sample_rate, tail_start, gate_floor)
    if trim_tail:
        clip = trim_quiet_tail(clip, sample_rate)

    clip = apply_fades(clip, sample_rate)
    clip = pad_min_duration(normalize(clip), sample_rate, 0.12)
    return clip


def normalize(samples: list[float]) -> list[float]:
    current_peak = peak(samples)
    if current_peak <= 0.000001:
        return samples

    scale = TARGET_PEAK / current_peak
    return [sample * scale for sample in samples]


def remove_dc(samples: list[float]) -> list[float]:
    if not samples:
        return samples

    mean = sum(samples) / len(samples)
    return [sample - mean for sample in samples]


def peak(samples: list[float]) -> float:
    return max((abs(sample) for sample in samples), default=0.0)


def rms(samples: list[float]) -> float:
    if not samples:
        return 0.0

    return math.sqrt(sum(sample * sample for sample in samples) / len(samples))


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


def slice_raw_clips(raw_dir: Path, output_dir: Path, max_per_source: int, pre_roll: float, duration: float) -> list[Candidate]:
    if output_dir.resolve() == FINAL_DRUM_DIR.resolve():
        raise ValueError("This tool refuses to write candidates into the final drum bank.")

    output_dir.mkdir(parents=True, exist_ok=True)
    candidates: list[Candidate] = []

    for source_path in sorted(raw_dir.glob("*.wav")):
        sample_rate, samples = read_wav_mono(source_path)
        onsets = detect_transient_candidates(samples, sample_rate, max_per_source)
        for candidate_index, onset in enumerate(onsets, start=1):
            start_seconds, end_seconds, clip = extract_candidate(samples, onset, sample_rate, pre_roll, duration)
            if not clip:
                continue

            output_name = f"{source_path.stem}_{candidate_index:02d}_{start_seconds:06.2f}s.wav"
            output_name = output_name.replace(" ", "_").replace("(", "").replace(")", "")
            write_wav(output_dir / output_name, clip)
            candidates.append(
                Candidate(
                    source_path.name,
                    candidate_index,
                    start_seconds,
                    end_seconds,
                    peak(clip),
                    rms(clip[: min(len(clip), int(sample_rate * 0.04))]),
                    output_name,
                )
            )

    write_manifest(output_dir / "manifest.csv", candidates)
    return candidates


def export_selection(
    raw_dir: Path,
    output_dir: Path,
    selection_file: Path,
    tail_start: float,
    tail_decay: float,
    gate_floor: float,
) -> list[Candidate]:
    if output_dir.resolve() == FINAL_DRUM_DIR.resolve():
        raise ValueError("This tool refuses to write candidates into the final drum bank.")

    output_dir.mkdir(parents=True, exist_ok=True)
    sources: dict[str, tuple[int, list[float]]] = {}
    candidates: list[Candidate] = []

    with selection_file.open("r", newline="", encoding="utf-8") as stream:
        reader = csv.DictReader(stream)
        for row_index, row in enumerate(reader, start=1):
            hit_id = row["hit_id"].strip()
            source_name = row["source_name"].strip()
            start_seconds = float(row["start_seconds"])
            end_seconds = float(row["end_seconds"])
            if end_seconds <= start_seconds:
                raise ValueError(f"Selection row {row_index} has an invalid time range.")

            if source_name not in sources:
                sources[source_name] = read_wav_mono(raw_dir / source_name)

            sample_rate, samples = sources[source_name]
            start = max(0, int(start_seconds * sample_rate))
            end = min(len(samples), int(end_seconds * sample_rate))
            clip = postprocess_candidate(
                samples[start:end],
                sample_rate,
                tail_start=tail_start,
                tail_decay=tail_decay,
                gate_floor=gate_floor,
                trim_tail=True,
            )
            output_name = f"{hit_id}.wav"
            write_wav(output_dir / output_name, clip)
            candidates.append(
                Candidate(
                    source_name,
                    row_index,
                    start_seconds,
                    end_seconds,
                    peak(clip),
                    rms(clip[: min(len(clip), int(sample_rate * 0.04))]),
                    output_name,
                )
            )

    write_manifest(output_dir / "manifest.csv", candidates)
    return candidates


def write_manifest(path: Path, candidates: list[Candidate]) -> None:
    with path.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(["output_name", "source_name", "index", "start_seconds", "end_seconds", "peak", "score"])
        for candidate in candidates:
            writer.writerow(
                [
                    candidate.output_name,
                    candidate.source_name,
                    candidate.index,
                    f"{candidate.start_seconds:.4f}",
                    f"{candidate.end_seconds:.4f}",
                    f"{candidate.peak:.4f}",
                    f"{candidate.score:.4f}",
                ]
            )


def main() -> int:
    parser = argparse.ArgumentParser(description="Slice raw Viking drum clips into audition candidate one-shots.")
    parser.add_argument("--raw-dir", type=Path, default=DEFAULT_RAW_DIR, help="Directory containing raw WAV clips.")
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR, help="Directory for candidate WAVs and manifest.")
    parser.add_argument("--max-per-source", type=int, default=18, help="Maximum candidate hits to export per source WAV.")
    parser.add_argument("--pre-roll", type=float, default=0.018, help="Seconds to include before detected attacks.")
    parser.add_argument("--duration", type=float, default=0.55, help="Maximum candidate duration in seconds.")
    parser.add_argument("--selection-file", type=Path, help="Optional CSV with hit_id, source_name, start_seconds, end_seconds.")
    parser.add_argument("--tail-start", type=float, default=0.035, help="Seconds to preserve before selected-cut tail damping.")
    parser.add_argument("--tail-decay", type=float, default=14.0, help="Selected-cut exponential tail damping strength.")
    parser.add_argument("--gate-floor", type=float, default=0.16, help="Minimum gain for the selected-cut soft tail gate.")
    args = parser.parse_args()

    if args.selection_file:
        candidates = export_selection(
            args.raw_dir,
            args.output_dir,
            args.selection_file,
            args.tail_start,
            args.tail_decay,
            args.gate_floor,
        )
    else:
        candidates = slice_raw_clips(args.raw_dir, args.output_dir, args.max_per_source, args.pre_roll, args.duration)

    print(f"Wrote {len(candidates)} candidates to {args.output_dir}")
    print(f"Manifest: {args.output_dir / 'manifest.csv'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
