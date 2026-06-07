using System;
using System.IO;
using System.Text;
using BepInEx.Logging;
using Bardheim.Assets;
using Bardheim.Notes;
using UnityEngine;

namespace Bardheim.Audio;

public static class EmbeddedFluteSampleFactory
{
    private const string ResourcePrefix = "Bardheim.Assets.Audio.Flute.";

    public static AudioClip? TryCreate(NoteDefinition note, ManualLogSource logger)
    {
        var resourceName = $"{ResourcePrefix}{note.Name}.wav";
        var data = EmbeddedAssetLoader.ReadResource(resourceName);
        if (data is null)
        {
            logger.LogWarning($"Flute note sample resource not found: {resourceName}");
            return null;
        }

        if (!TryReadPcm16Wav(data, out var samples, out var channels, out var sampleRate, out var error))
        {
            logger.LogWarning($"Could not load flute note sample {note.Name}: {error}");
            return null;
        }

        var frameCount = samples.Length / channels;
        var samplePosition = 0;
        var clip = AudioClip.Create(
            $"Flute_{note.Name}_Sample",
            frameCount,
            channels,
            sampleRate,
            false,
            target => Fill(target, samples, ref samplePosition));

        logger.LogInfo($"Loaded flute note sample {note.Name}.");
        return clip;
    }

    private static bool TryReadPcm16Wav(
        byte[] data,
        out float[] samples,
        out int channels,
        out int sampleRate,
        out string error)
    {
        samples = Array.Empty<float>();
        channels = 0;
        sampleRate = 0;
        error = string.Empty;

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream, Encoding.ASCII);

        if (ReadFourCc(reader) != "RIFF")
        {
            error = "Missing RIFF header.";
            return false;
        }

        _ = reader.ReadInt32();
        if (ReadFourCc(reader) != "WAVE")
        {
            error = "Missing WAVE header.";
            return false;
        }

        ushort audioFormat = 0;
        ushort bitsPerSample = 0;
        byte[]? sampleBytes = null;

        while (stream.Position + 8 <= stream.Length)
        {
            var chunkId = ReadFourCc(reader);
            var chunkSize = reader.ReadInt32();
            var chunkStart = stream.Position;

            if (chunkId == "fmt ")
            {
                audioFormat = reader.ReadUInt16();
                channels = reader.ReadUInt16();
                sampleRate = reader.ReadInt32();
                _ = reader.ReadInt32();
                _ = reader.ReadUInt16();
                bitsPerSample = reader.ReadUInt16();
            }
            else if (chunkId == "data")
            {
                sampleBytes = reader.ReadBytes(chunkSize);
            }

            stream.Position = chunkStart + chunkSize + (chunkSize % 2);
        }

        if (audioFormat != 1)
        {
            error = $"Unsupported WAV format {audioFormat}; expected PCM.";
            return false;
        }

        if (channels < 1)
        {
            error = "Invalid channel count.";
            return false;
        }

        if (sampleRate <= 0)
        {
            error = "Invalid sample rate.";
            return false;
        }

        if (bitsPerSample != 16)
        {
            error = $"Unsupported bit depth {bitsPerSample}; expected 16-bit.";
            return false;
        }

        if (sampleBytes is null || sampleBytes.Length == 0)
        {
            error = "Missing sample data.";
            return false;
        }

        samples = new float[sampleBytes.Length / 2];
        for (var index = 0; index < samples.Length; index++)
        {
            var pcm = BitConverter.ToInt16(sampleBytes, index * 2);
            samples[index] = pcm / 32768.0f;
        }

        return true;
    }

    private static string ReadFourCc(BinaryReader reader)
    {
        return Encoding.ASCII.GetString(reader.ReadBytes(4));
    }

    private static void Fill(float[] target, float[] source, ref int samplePosition)
    {
        for (var index = 0; index < target.Length; index++)
        {
            target[index] = samplePosition < source.Length ? source[samplePosition] : 0.0f;
            samplePosition++;
        }
    }
}
