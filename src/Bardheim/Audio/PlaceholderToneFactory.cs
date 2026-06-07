using Bardheim.Notes;
using UnityEngine;

namespace Bardheim.Audio;

public static class PlaceholderToneFactory
{
    private const int SampleRate = 44100;
    private const float DurationSeconds = 0.32f;

    public static AudioClip Create(NoteDefinition note)
    {
        var sampleCount = Mathf.CeilToInt(SampleRate * DurationSeconds);
        var sampleIndex = 0;
        return AudioClip.Create(
            $"Lyre_{note.Name}",
            sampleCount,
            1,
            SampleRate,
            false,
            data => Fill(data, note.FrequencyHz, sampleCount, ref sampleIndex));
    }

    private static void Fill(float[] data, float frequencyHz, int sampleCount, ref int sampleIndex)
    {
        for (var index = 0; index < data.Length; index++)
        {
            if (sampleIndex >= sampleCount)
            {
                data[index] = 0.0f;
                continue;
            }

            var time = sampleIndex / (float)SampleRate;
            var envelope = CreateEnvelope(sampleIndex, sampleCount);
            data[index] = Mathf.Sin(2.0f * Mathf.PI * frequencyHz * time) * envelope;
            sampleIndex++;
        }
    }

    private static float CreateEnvelope(int sampleIndex, int sampleCount)
    {
        var normalized = sampleIndex / (float)sampleCount;
        if (normalized < 0.04f)
        {
            return normalized / 0.04f;
        }

        if (normalized > 0.82f)
        {
            return Mathf.Clamp01((1.0f - normalized) / 0.18f);
        }

        return 1.0f;
    }
}
