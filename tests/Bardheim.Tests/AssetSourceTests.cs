using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace Bardheim.Tests;

public sealed class AssetSourceTests
{
    private static readonly string[] ExpectedNotes =
    {
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
        "Bb3",
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
        "Bb4",
        "C5",
        "C#5",
        "D5"
    };

    private static readonly string[] BuiltInTuningNotes =
    {
        "D3",
        "E3",
        "F3",
        "G3",
        "A3",
        "Bb3",
        "C4",
        "D4",
        "E4",
        "F4",
        "G4"
    };

    private static readonly string[] ExpectedDrumHits =
    {
        "kick",
        "snare",
        "rim",
        "clap",
        "muted",
        "open",
        "low_tom",
        "high_tom",
        "low_kick",
        "electric_snare",
        "pedal_hat",
        "crash",
        "ride",
        "mid_tom",
        "cowbell",
        "tambourine"
    };

    private static readonly string[] ExpectedFluteNotes =
    {
        "D4",
        "D#4",
        "E4",
        "F4",
        "F#4",
        "G4",
        "G#4",
        "A4",
        "Bb4",
        "B4",
        "C5",
        "C#5",
        "D5",
        "D#5",
        "E5",
        "F5",
        "F#5",
        "G5",
        "G#5",
        "A5",
        "Bb5",
        "B5"
    };

    [Fact]
    public void AudioSourceContainsExpectedMonoPcmWavs()
    {
        var audioDir = Path.Combine(GetModRoot(), "assets-src", "audio", "notes");
        var actualNotes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var wavPath in Directory.GetFiles(audioDir, "*.wav"))
        {
            var note = Path.GetFileNameWithoutExtension(wavPath);
            actualNotes.Add(note);
            using var reader = new BinaryReader(File.OpenRead(wavPath), Encoding.ASCII);

            Assert.Equal("RIFF", ReadFourCc(reader));
            _ = reader.ReadInt32();
            Assert.Equal("WAVE", ReadFourCc(reader));

            var formatSeen = false;
            var dataSeen = false;
            while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
            {
                var chunkId = ReadFourCc(reader);
                var chunkSize = reader.ReadInt32();
                var chunkStart = reader.BaseStream.Position;

                if (chunkId == "fmt ")
                {
                    formatSeen = true;
                    Assert.Equal((ushort)1, reader.ReadUInt16());
                    Assert.Equal((ushort)1, reader.ReadUInt16());
                    Assert.Equal(44100, reader.ReadInt32());
                    _ = reader.ReadInt32();
                    _ = reader.ReadUInt16();
                    Assert.Equal((ushort)16, reader.ReadUInt16());
                }
                else if (chunkId == "data")
                {
                    dataSeen = true;
                    Assert.True(chunkSize > 0);
                }

                reader.BaseStream.Position = chunkStart + chunkSize + (chunkSize % 2);
            }

            Assert.True(formatSeen);
            Assert.True(dataSeen);
        }

        foreach (var expectedNote in ExpectedNotes)
        {
            Assert.Contains(expectedNote, actualNotes);
        }
    }

    [Fact]
    public void AudioSourceDocumentsUserClipPreparation()
    {
        var audioDir = Path.Combine(GetModRoot(), "assets-src", "audio");
        var scriptPath = Path.Combine(audioDir, "prepare_user_clip_samples.py");
        var licensePath = Path.Combine(audioDir, "LICENSES.md");

        Assert.True(File.Exists(scriptPath));
        Assert.Contains("user-supplied", File.ReadAllText(licensePath));
    }

    [Fact]
    public void DrumAudioSourceContainsExpectedPercussiveMonoPcmWavs()
    {
        var drumDir = Path.Combine(GetModRoot(), "assets-src", "audio", "drums");
        var actualHits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var wavPath in Directory.GetFiles(drumDir, "*.wav"))
        {
            var hit = Path.GetFileNameWithoutExtension(wavPath);
            actualHits.Add(hit);
            var info = ReadPcm16WavInfo(wavPath);
            var durationSeconds = info.Samples.Length / (double)info.SampleRate;
            var peak = MaxAbs(info.Samples);
            var attackPeak = MaxAbs(info.Samples, 0, Math.Min(info.Samples.Length, (int)(info.SampleRate * 0.03)));
            var tailRms = Rms(info.Samples, Math.Max(0, info.Samples.Length - (int)(info.SampleRate * 0.08)), info.Samples.Length);

            Assert.Equal(44100, info.SampleRate);
            Assert.InRange(durationSeconds, 0.12, 0.90);
            Assert.InRange(peak, 0.55, 0.86);
            Assert.True(attackPeak >= peak * 0.35, $"{hit} does not have enough early attack.");
            Assert.True(tailRms <= peak * 0.18, $"{hit} has too much tail energy: tail={tailRms:F4}, peak={peak:F4}.");
        }

        foreach (var expectedHit in ExpectedDrumHits)
        {
            Assert.Contains(expectedHit, actualHits);
        }

        Assert.Equal(ExpectedDrumHits.Length, actualHits.Count);
    }

    [Fact]
    public void FluteAudioSourceContainsExpectedMelodicMonoPcmWavs()
    {
        var fluteDir = Path.Combine(GetModRoot(), "assets-src", "audio", "flute");
        var actualNotes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var wavPath in Directory.GetFiles(fluteDir, "*.wav"))
        {
            var note = Path.GetFileNameWithoutExtension(wavPath);
            actualNotes.Add(note);
            var info = ReadPcm16WavInfo(wavPath);
            var durationSeconds = info.Samples.Length / (double)info.SampleRate;
            var peak = MaxAbs(info.Samples);
            var tailRms = Rms(info.Samples, Math.Max(0, info.Samples.Length - (int)(info.SampleRate * 0.08)), info.Samples.Length);

            Assert.Equal(44100, info.SampleRate);
            Assert.InRange(durationSeconds, 0.42, 0.52);
            Assert.InRange(peak, 0.45, 0.86);
            Assert.True(tailRms <= peak * 0.18, $"{note} has too much tail energy: tail={tailRms:F4}, peak={peak:F4}.");
        }

        foreach (var expectedNote in ExpectedFluteNotes)
        {
            Assert.Contains(expectedNote, actualNotes);
        }

        Assert.Equal(ExpectedFluteNotes.Length, actualNotes.Count);
    }

    [Fact]
    public void FluteSamplesDoNotContainAudiblePreNotesOrSecondAttacks()
    {
        var fluteDir = Path.Combine(GetModRoot(), "assets-src", "audio", "flute");

        foreach (var note in ExpectedFluteNotes)
        {
            var samples = ReadPcm16Samples(Path.Combine(fluteDir, $"{note}.wav"));
            var firstAttackPeak = MaxRms(samples, 0, 353, 88, 1323, 6174);
            var latePeak = MaxRms(samples, 0, 353, 88, 7056, Math.Min(samples.Length - 353, 17640));

            Assert.True(
                latePeak <= firstAttackPeak * 2.10,
                $"{note} has a strong second flute attack: attack={firstAttackPeak:F4}, late={latePeak:F4}.");
        }
    }

    [Fact]
    public void FluteSamplesKeepSustainedBreathyBody()
    {
        var fluteDir = Path.Combine(GetModRoot(), "assets-src", "audio", "flute");

        foreach (var note in ExpectedFluteNotes)
        {
            var samples = ReadPcm16Samples(Path.Combine(fluteDir, $"{note}.wav"));
            var attackRms = Rms(samples, 1323, 6174);
            var bodyRms = Rms(samples, 7056, 15876);

            Assert.True(
                bodyRms >= attackRms * 0.45,
                $"{note} decays like a plucked instrument instead of a sustained flute: attack={attackRms:F4}, body={bodyRms:F4}.");
        }
    }

    [Fact]
    public void FluteAudioAuditScriptUsesPitchAndCoverageChecks()
    {
        var scriptPath = Path.Combine(GetModRoot(), "assets-src", "audio", "ensure_flute_note_samples.py");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("FLUTE_NOTES", script);
        Assert.Contains("cents", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pitch", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MAX_SEGMENT_MEDIAN_CENTS", script);
        Assert.Contains("MAX_PITCH_DRIFT_CENTS", script);
        Assert.Contains("MAX_SECONDARY_PULSE_RATIO", script);
        Assert.Contains("MIN_FLUTE_BODY_RATIO", script);
        Assert.Contains("estimate_flute_body_ratio", script);
        Assert.Contains("estimate_secondary_pulse_ratio", script);
        Assert.Contains("estimate_pitch_segments", script);
        Assert.Contains("breath", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--generate", script);
    }

    [Fact]
    public void FluteAudioPreparationScriptDocumentsSourceSampleWorkflow()
    {
        var scriptPath = Path.Combine(GetModRoot(), "assets-src", "audio", "prepare_flute_samples.py");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("flute-samples", script);
        Assert.Contains("flute_sample_manifest.csv", script);
        Assert.Contains("PHILHARMONIA_FLUTE_DIR", script);
        Assert.Contains("05_mezzo-forte_normal", script);
        Assert.Contains("ffmpeg", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pitch", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("retune", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("estimate_rendered_pitch_segments", script);
        Assert.Contains("segment_median_cents", script);
        Assert.Contains("segment_drift_cents", script);
        Assert.Contains("secondary_pulse_ratio", script);
        Assert.Contains("body_sustain_ratio", script);
        Assert.Contains("PRE_ROLL_SECONDS = 0.0", script);
    }

    [Fact]
    public void FluteSamplesUseDirectPhilharmoniaSourcesForEveryEmbeddedNote()
    {
        var manifestPath = Path.Combine(GetModRoot(), "assets-src", "audio", "flute", "flute_sample_manifest.csv");
        var manifest = File.ReadAllText(manifestPath);

        foreach (var expectedNote in ExpectedFluteNotes)
        {
            var sourceNote = expectedNote.Replace("#", "s").Replace("Bb", "As");
            var manifestLine = Assert.Single(
                manifest.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
                line => line.StartsWith(expectedNote + ",", StringComparison.Ordinal));

            Assert.Contains($",{sourceNote},", manifestLine);
            Assert.Contains($"flute_{sourceNote}_", manifestLine);
            Assert.Contains("_normal.mp3", manifestLine);
        }
    }

    [Fact]
    public void ProjectEmbedsFluteSamplesWithSeparateResourcePrefix()
    {
        var project = File.ReadAllText(Path.Combine(
            GetModRoot(),
            "src",
            "Bardheim",
            "Bardheim.csproj"));

        Assert.Contains("assets-src\\audio\\flute", project);
        Assert.Contains("Bardheim.Assets.Audio.Flute.", project);

        foreach (var expectedNote in ExpectedFluteNotes)
        {
            Assert.Contains($"flute\\{expectedNote}.wav", project);
        }
    }

    [Fact]
    public void DrumAudioAuditScriptUsesPercussiveChecksInsteadOfPitchTuning()
    {
        var scriptPath = Path.Combine(GetModRoot(), "assets-src", "audio", "ensure_drum_hit_samples.py");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("DRUM_HITS", script);
        Assert.Contains("tail", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("frame drum", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hide", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wood", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rattle", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cents", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pitch", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("semitone", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DrumAudioAuditScriptPreservesCuratedSamplesByDefault()
    {
        var scriptPath = Path.Combine(GetModRoot(), "assets-src", "audio", "ensure_drum_hit_samples.py");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("--generate-procedural", script);
        Assert.Contains("args.generate_procedural", script);
        Assert.DoesNotContain("if not args.audit_only", script);
    }

    [Fact]
    public void DrumRawClipSlicerDocumentsCandidateExtractionWorkflow()
    {
        var scriptPath = Path.Combine(GetModRoot(), "assets-src", "audio", "slice_drum_raw_clips.py");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("sounds/drums", script.Replace("\\", "/"));
        Assert.Contains("candidate", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("detect_transient_candidates", script);
        Assert.Contains("normalize", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fade", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("manifest", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--raw-dir", script);
        Assert.Contains("--output-dir", script);
        Assert.Contains("--selection-file", script);
        Assert.Contains("refuses to write candidates into the final drum bank", script);

        var selectionPath = Path.Combine(GetModRoot(), "assets-src", "audio", "drum_candidate_selection.csv");
        var selection = File.ReadAllText(selectionPath);
        foreach (var expectedHit in ExpectedDrumHits)
        {
            Assert.Contains(expectedHit + ",", selection);
        }
    }

    [Fact]
    public void AudioSamplesDoNotContainStrongSecondaryPlucks()
    {
        var audioDir = Path.Combine(GetModRoot(), "assets-src", "audio", "notes");

        foreach (var note in BuiltInTuningNotes)
        {
            var samples = ReadPcm16Samples(Path.Combine(audioDir, $"{note}.wav"));
            var earlyPeak = MaxRms(samples, 0, 529, 176, 0, 5292);
            var latePeak = MaxRms(samples, 0, 529, 176, 7938, samples.Length - 529);

            Assert.True(
                latePeak <= earlyPeak * 0.35,
                $"{note} has a strong secondary pluck: early={earlyPeak:F4}, late={latePeak:F4}.");
        }
    }

    private static string GetModRoot([CallerFilePath] string sourceFile = "")
    {
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", ".."));
    }

    private static string ReadFourCc(BinaryReader reader)
    {
        return Encoding.ASCII.GetString(reader.ReadBytes(4));
    }

    private static short[] ReadPcm16Samples(string wavPath)
    {
        return ReadPcm16WavInfo(wavPath).Samples;
    }

    private static WavInfo ReadPcm16WavInfo(string wavPath)
    {
        using var reader = new BinaryReader(File.OpenRead(wavPath), Encoding.ASCII);

        Assert.Equal("RIFF", ReadFourCc(reader));
        _ = reader.ReadInt32();
        Assert.Equal("WAVE", ReadFourCc(reader));

        var sampleRate = 0;
        while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
        {
            var chunkId = ReadFourCc(reader);
            var chunkSize = reader.ReadInt32();
            var chunkStart = reader.BaseStream.Position;

            if (chunkId == "fmt ")
            {
                Assert.Equal((ushort)1, reader.ReadUInt16());
                Assert.Equal((ushort)1, reader.ReadUInt16());
                sampleRate = reader.ReadInt32();
                _ = reader.ReadInt32();
                _ = reader.ReadUInt16();
                Assert.Equal((ushort)16, reader.ReadUInt16());
            }

            if (chunkId == "data")
            {
                Assert.True(sampleRate > 0);
                var sampleCount = chunkSize / 2;
                var samples = new short[sampleCount];
                for (var index = 0; index < sampleCount; index++)
                {
                    samples[index] = reader.ReadInt16();
                }

                return new WavInfo(sampleRate, samples);
            }

            reader.BaseStream.Position = chunkStart + chunkSize + (chunkSize % 2);
        }

        throw new InvalidDataException($"No data chunk found in {wavPath}.");
    }

    private static double MaxAbs(short[] samples)
    {
        return MaxAbs(samples, 0, samples.Length);
    }

    private static double MaxAbs(short[] samples, int start, int end)
    {
        var max = 0.0;
        for (var index = start; index < end; index++)
        {
            max = Math.Max(max, Math.Abs(samples[index] / 32768.0));
        }

        return max;
    }

    private static double Rms(short[] samples, int start, int end)
    {
        if (end <= start)
        {
            return 0.0;
        }

        var sum = 0.0;
        for (var index = start; index < end; index++)
        {
            var value = samples[index] / 32768.0;
            sum += value * value;
        }

        return Math.Sqrt(sum / (end - start));
    }

    private static double MaxRms(short[] samples, int channelOffset, int frameSize, int hopSize, int start, int end)
    {
        var max = 0.0;
        for (var offset = start; offset <= end; offset += hopSize)
        {
            var sum = 0.0;
            for (var index = 0; index < frameSize; index++)
            {
                var value = samples[offset + index + channelOffset] / 32768.0;
                sum += value * value;
            }

            max = Math.Max(max, Math.Sqrt(sum / frameSize));
        }

        return max;
    }

    private sealed class WavInfo
    {
        public WavInfo(int sampleRate, short[] samples)
        {
            SampleRate = sampleRate;
            Samples = samples;
        }

        public int SampleRate { get; }

        public short[] Samples { get; }
    }
}
