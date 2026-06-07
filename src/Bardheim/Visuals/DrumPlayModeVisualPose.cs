using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using Bardheim.Instruments;
using Bardheim.Items;
using UnityEngine;

namespace Bardheim.Visuals;

internal sealed class DrumPlayModeVisualPose : MonoBehaviour
{
    private static readonly HashSet<DrumPlayModeVisualPose> Instances = new();
    private static readonly Vector3 DefaultCarryLocalPosition = new Vector3(0.05f, 0.10f, 0.30f);
    private static readonly Vector3 DefaultCarryLocalEuler = new Vector3(-15.0f, -55.0f, 85.0f);
    private const float PositionStep = 0.05f;
    private const float RotationStep = 5.0f;
    private const string PoseDirectoryName = "Bardheim";
    private const string PoseFilename = "drum-hand-visual-pose.txt";

    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private Vector3 _baseLocalScale;
    private Vector3 _carryLocalPosition = DefaultCarryLocalPosition;
    private Vector3 _carryLocalEuler = DefaultCarryLocalEuler;
    private bool _capturedBasePose;
    private bool _loadedPose;

    public static void UpdateCalibration(bool enabled, string? activeInstrumentId, ManualLogSource logger)
    {
        if (!enabled || activeInstrumentId != InstrumentCatalog.DrumsId)
        {
            return;
        }

        foreach (var instance in Instances)
        {
            if (instance is null || !instance.CanApplyPose())
            {
                continue;
            }

            instance.UpdateCalibrationInput(logger);
        }
    }

    private void Awake()
    {
        CaptureBasePose();
        LoadPoseIfNeeded();
        ApplyCarryPose();
    }

    private void OnEnable()
    {
        CaptureBasePose();
        LoadPoseIfNeeded();
        ApplyCarryPose();
        Instances.Add(this);
    }

    private void OnDisable()
    {
        Instances.Remove(this);
    }

    private bool CanApplyPose()
    {
        return gameObject.activeInHierarchy &&
               transform.root is not null &&
               !transform.root.name.Equals(DrumItemRegistration.PrefabName, StringComparison.Ordinal);
    }

    private void CaptureBasePose()
    {
        if (_capturedBasePose)
        {
            return;
        }

        _baseLocalPosition = transform.localPosition;
        _baseLocalRotation = transform.localRotation;
        _baseLocalScale = transform.localScale;
        _capturedBasePose = true;
    }

    private void LoadPoseIfNeeded()
    {
        if (_loadedPose)
        {
            return;
        }

        _loadedPose = true;
        var path = PosePath();
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.StartsWith("position=", StringComparison.Ordinal))
                {
                    _carryLocalPosition = ParseVector3(line.Substring("position=".Length), _carryLocalPosition);
                }
                else if (line.StartsWith("euler=", StringComparison.Ordinal))
                {
                    _carryLocalEuler = ParseVector3(line.Substring("euler=".Length), _carryLocalEuler);
                }
            }
        }
        catch (Exception)
        {
            _carryLocalPosition = DefaultCarryLocalPosition;
            _carryLocalEuler = DefaultCarryLocalEuler;
        }
    }

    private void ApplyCarryPose()
    {
        if (!_capturedBasePose)
        {
            return;
        }

        transform.localPosition = _baseLocalPosition + _carryLocalPosition;
        transform.localRotation = _baseLocalRotation * Quaternion.Euler(_carryLocalEuler);
        transform.localScale = _baseLocalScale;
    }

    private void UpdateCalibrationInput(ManualLogSource logger)
    {
        var positionDelta = Vector3.zero;
        var rotationDelta = Vector3.zero;
        var rotate = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            if (rotate)
            {
                rotationDelta.y -= RotationStep;
            }
            else
            {
                positionDelta.x -= PositionStep;
            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad6))
        {
            if (rotate)
            {
                rotationDelta.y += RotationStep;
            }
            else
            {
                positionDelta.x += PositionStep;
            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            if (rotate)
            {
                rotationDelta.x -= RotationStep;
            }
            else
            {
                positionDelta.y += PositionStep;
            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            if (rotate)
            {
                rotationDelta.x += RotationStep;
            }
            else
            {
                positionDelta.y -= PositionStep;
            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad7))
        {
            if (rotate)
            {
                rotationDelta.z -= RotationStep;
            }
            else
            {
                positionDelta.z -= PositionStep;
            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad9))
        {
            if (rotate)
            {
                rotationDelta.z += RotationStep;
            }
            else
            {
                positionDelta.z += PositionStep;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            rotationDelta.y -= RotationStep;
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            rotationDelta.y += RotationStep;
        }

        if (Input.GetKeyDown(KeyCode.Semicolon))
        {
            rotationDelta.z -= RotationStep;
        }

        if (Input.GetKeyDown(KeyCode.Quote))
        {
            rotationDelta.z += RotationStep;
        }

        if (Input.GetKeyDown(KeyCode.Comma))
        {
            rotationDelta.x -= RotationStep;
        }

        if (Input.GetKeyDown(KeyCode.Period))
        {
            rotationDelta.x += RotationStep;
        }

        if (positionDelta == Vector3.zero && rotationDelta == Vector3.zero)
        {
            return;
        }

        _carryLocalPosition += positionDelta;
        _carryLocalEuler += rotationDelta;
        ApplyCarryPose();
        SavePose(logger);
    }

    private void SavePose(ManualLogSource logger)
    {
        var path = PosePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(
            path,
            new[]
            {
                "position=" + FormatVector(_carryLocalPosition),
                "euler=" + FormatVector(_carryLocalEuler)
            });

        logger.LogInfo(
            "Drum visual pose saved: " +
            $"position={FormatVector(_carryLocalPosition)} euler={FormatVector(_carryLocalEuler)} file={path}");
    }

    private static string PosePath()
    {
        return Path.Combine(Paths.ConfigPath, PoseDirectoryName, PoseFilename);
    }

    private static string FormatVector(Vector3 value)
    {
        return string.Join(
            ",",
            value.x.ToString("F3", CultureInfo.InvariantCulture),
            value.y.ToString("F3", CultureInfo.InvariantCulture),
            value.z.ToString("F3", CultureInfo.InvariantCulture));
    }

    private static Vector3 ParseVector3(string value, Vector3 fallback)
    {
        var parts = value.Split(',');
        if (parts.Length != 3)
        {
            return fallback;
        }

        return float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
               float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
               float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)
            ? new Vector3(x, y, z)
            : fallback;
    }
}
