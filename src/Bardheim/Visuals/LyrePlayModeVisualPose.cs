using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Bardheim.Items;
using UnityEngine;

namespace Bardheim.Visuals;

internal sealed class LyrePlayModeVisualPose : MonoBehaviour
{
    private static readonly HashSet<LyrePlayModeVisualPose> Instances = new();
    private static readonly Vector3 CarryLocalPosition = new Vector3(0.05f, 0.00f, -0.05f);
    private static readonly Vector3 CarryLocalEuler = new Vector3(10.0f, -15.0f, 10.0f);
    private const float PositionStep = 0.05f;
    private const float RotationStep = 5.0f;

    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private Vector3 _baseLocalScale;
    private Vector3 _carryLocalPosition = CarryLocalPosition;
    private Vector3 _carryLocalEuler = CarryLocalEuler;
    private bool _capturedBasePose;

    public static void UpdateCalibration(bool enabled, ManualLogSource logger)
    {
        if (!enabled)
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
        ApplyCarryPose();
    }

    private void OnEnable()
    {
        CaptureBasePose();
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
               !transform.root.name.Equals(LyreItemRegistration.PrefabName, StringComparison.Ordinal);
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

    private void UpdateCalibrationInput(ManualLogSource logger)
    {
        var positionDelta = Vector3.zero;
        var rotationDelta = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            positionDelta.x -= PositionStep;
        }

        if (Input.GetKeyDown(KeyCode.Keypad6))
        {
            positionDelta.x += PositionStep;
        }

        if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            positionDelta.y += PositionStep;
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            positionDelta.y -= PositionStep;
        }

        if (Input.GetKeyDown(KeyCode.Keypad7))
        {
            positionDelta.z -= PositionStep;
        }

        if (Input.GetKeyDown(KeyCode.Keypad9))
        {
            positionDelta.z += PositionStep;
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
        LogCalibrationConstants(logger);
    }

    private void LogCalibrationConstants(ManualLogSource logger)
    {
        logger.LogInfo(
            "Lyre carry pose calibration constants: " +
            $"CarryLocalPosition = new Vector3({_carryLocalPosition.x:F2}f, {_carryLocalPosition.y:F2}f, {_carryLocalPosition.z:F2}f); " +
            $"CarryLocalEuler = new Vector3({_carryLocalEuler.x:F1}f, {_carryLocalEuler.y:F1}f, {_carryLocalEuler.z:F1}f)");
    }
}
