using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Bardheim.Visuals;
using UnityEngine;

namespace Bardheim.Items;

internal static class DrumHandDrumVisualProbe
{
    private const string VisualName = "Bardheim_OneHandedHangingDrumVisual";
    private const int BodySegments = 24;
    private const int RodSegments = 8;

    public static void Apply(GameObject itemPrefab, ManualLogSource logger)
    {
        if (itemPrefab is null)
        {
            logger.LogWarning("Drum one-handed visual probe skipped because the cloned item prefab is null.");
            return;
        }

        var mesh = BuildDrumMesh();
        var materials = CreateMaterials();
        var renderers = itemPrefab.GetComponentsInChildren<Renderer>(true);
        var target = SelectTargetRenderer(itemPrefab);

        if (target is not null)
        {
            target.Filter!.sharedMesh = mesh;
            target.Renderer.sharedMaterials = materials;
            target.Renderer.gameObject.name = VisualName;
            EnsurePlayModePose(target.Renderer.gameObject);
            target.Renderer.enabled = true;

            foreach (var renderer in renderers)
            {
                if (renderer != target.Renderer)
                {
                    renderer.enabled = false;
                }
            }

            logger.LogInfo(
                $"Applied one-handed hanging drum visual by replacing renderer path={GetPath(itemPrefab.transform, target.Renderer.transform)} " +
                $"mesh={mesh.name} materials={materials.Length} disabledRenderers={Math.Max(0, renderers.Length - 1)}.");
            return;
        }

        var fallback = new GameObject(VisualName);
        fallback.transform.SetParent(itemPrefab.transform, false);
        fallback.transform.localPosition = Vector3.zero;
        fallback.transform.localRotation = Quaternion.identity;
        fallback.transform.localScale = Vector3.one;

        var meshFilter = fallback.AddComponent<MeshFilter>();
        var meshRenderer = fallback.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterials = materials;
        EnsurePlayModePose(fallback);

        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }

        logger.LogWarning(
            $"Applied one-handed hanging drum visual as fallback child path={VisualName}; no MeshRenderer with MeshFilter was found to replace in place.");
    }

    private static RendererTarget? SelectTargetRenderer(GameObject itemPrefab)
    {
        var candidates = itemPrefab
            .GetComponentsInChildren<MeshRenderer>(true)
            .Select(renderer => new RendererTarget(renderer, renderer.GetComponent<MeshFilter>()))
            .Where(candidate => candidate.Filter is not null)
            .OrderByDescending(candidate => candidate.Renderer.enabled)
            .ThenBy(candidate => GetPath(itemPrefab.transform, candidate.Renderer.transform), StringComparer.Ordinal)
            .ToArray();

        return candidates.Length == 0 ? null : candidates[0];
    }

    private static void EnsurePlayModePose(GameObject visual)
    {
        if (visual.GetComponent<DrumPlayModeVisualPose>() is null)
        {
            visual.AddComponent<DrumPlayModeVisualPose>();
        }
    }

    private static Mesh BuildDrumMesh()
    {
        var builder = new MeshBuilder(materialCount: 6);
        var drumCenter = new Vector3(0.0f, -0.42f, 0.0f);

        builder.AddCylinderZ(drumCenter, radiusX: 0.56f, radiusY: 0.52f, depth: 0.42f, segments: BodySegments, materialSide: 0, materialCap: 1);
        builder.AddCylinderZ(drumCenter + new Vector3(0.0f, 0.0f, 0.235f), radiusX: 0.585f, radiusY: 0.545f, depth: 0.055f, segments: BodySegments, materialSide: 4, materialCap: 1);
        builder.AddCylinderZ(drumCenter + new Vector3(0.0f, 0.0f, -0.235f), radiusX: 0.585f, radiusY: 0.545f, depth: 0.055f, segments: BodySegments, materialSide: 4, materialCap: 1);

        AddOutsideShellBand(builder, drumCenter);
        AddRaisedOutsideBands(builder, drumCenter);
        AddSolidSideShellPanels(builder, drumCenter);
        AddSideStaves(builder, drumCenter);
        AddRimBinding(builder, drumCenter);
        AddLacing(builder, drumCenter);
        AddSideCrossBraces(builder, drumCenter);
        AddLaceKnots(builder, drumCenter);
        AddExtraEdgeTabs(builder, drumCenter);
        AddBroadHidePanelVariation(builder, drumCenter);
        AddIrregularHidePatches(builder, drumCenter);
        AddTopGrip(builder, drumCenter);
        AddSideHandles(builder, drumCenter);
        AddRaisedKnotworkPaint(builder, drumCenter);

        var mesh = new Mesh
        {
            name = "Bardheim_GeneratedOneHandedHangingDrum"
        };
        builder.ApplyTo(mesh);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddSolidSideShellPanels(MeshBuilder builder, Vector3 center)
    {
        for (var index = 0; index < 18; index++)
        {
            var angle = Mathf.PI * 2.0f * index / 18.0f;
            var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
            var tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);
            var offset = 1.0f + 0.025f * Mathf.Sin(index * 1.7f);
            var panelCenter = center + new Vector3(radial.x * 0.535f * offset, radial.y * 0.500f * offset, 0.0f);
            var material = index % 3 == 0 ? 3 : 0;
            builder.AddOrientedBox(panelCenter, radial, tangent, Vector3.forward, new Vector3(0.070f, 0.125f, 0.430f), material);
        }
    }

    private static void AddOutsideShellBand(MeshBuilder builder, Vector3 center)
    {
        builder.AddCylinderSideZ(center, radiusX: 0.625f, radiusY: 0.585f, depth: 0.460f, segments: BodySegments, materialSide: 3);
        builder.AddCylinderSideZ(center, radiusX: 0.585f, radiusY: 0.545f, depth: 0.420f, segments: BodySegments, materialSide: 0);
    }

    private static void AddRaisedOutsideBands(MeshBuilder builder, Vector3 center)
    {
        builder.AddCylinderSideZ(center, radiusX: 0.655f, radiusY: 0.615f, depth: 0.140f, segments: BodySegments, materialSide: 4);
        builder.AddCylinderSideZ(center + new Vector3(0.0f, 0.0f, 0.185f), radiusX: 0.645f, radiusY: 0.605f, depth: 0.060f, segments: BodySegments, materialSide: 4);
        builder.AddCylinderSideZ(center + new Vector3(0.0f, 0.0f, -0.185f), radiusX: 0.645f, radiusY: 0.605f, depth: 0.060f, segments: BodySegments, materialSide: 4);

        for (var index = 0; index < 12; index++)
        {
            var angle = Mathf.PI * 2.0f * index / 12.0f;
            var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
            var tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);
            var buckleCenter = center + new Vector3(radial.x * 0.665f, radial.y * 0.625f, 0.0f);
            builder.AddOrientedBox(buckleCenter, radial, tangent, Vector3.forward, new Vector3(0.026f, 0.070f, 0.040f), index % 2 == 0 ? 2 : 4);
        }
    }

    private static void AddSideStaves(MeshBuilder builder, Vector3 center)
    {
        for (var index = 0; index < 12; index++)
        {
            var angle = Mathf.PI * 2.0f * index / 12.0f;
            var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
            var tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);
            var staveCenter = center + new Vector3(radial.x * 0.595f, radial.y * 0.555f, 0.0f);
            builder.AddOrientedBox(staveCenter, radial, tangent, Vector3.forward, new Vector3(0.040f, 0.090f, 0.430f), index % 2 == 0 ? 3 : 0);
            builder.AddRod(
                staveCenter + tangent * -0.035f + Vector3.forward * 0.205f,
                staveCenter + tangent * -0.035f + Vector3.forward * -0.205f,
                0.005f,
                4,
                4);
        }
    }

    private static void AddRimBinding(MeshBuilder builder, Vector3 center)
    {
        AddEllipseRing(builder, center, 0.610f, 0.570f, 0.270f, 0.022f, 4);
        AddEllipseRing(builder, center, 0.610f, 0.570f, -0.270f, 0.022f, 4);
        AddEllipseRing(builder, center, 0.500f, 0.470f, 0.285f, 0.011f, 2);
        AddEllipseRing(builder, center, 0.500f, 0.470f, -0.285f, 0.011f, 2);

        for (var index = 0; index < 16; index++)
        {
            var angle = Mathf.PI * 2.0f * index / 16.0f;
            var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
            var tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);
            var tack = center + new Vector3(radial.x * 0.500f, radial.y * 0.470f, 0.300f);
            builder.AddOrientedBox(tack, radial, tangent, Vector3.forward, new Vector3(0.026f, 0.038f, 0.014f), 2);
        }
    }

    private static void AddLacing(MeshBuilder builder, Vector3 center)
    {
        for (var index = 0; index < 14; index++)
        {
            var angle0 = Mathf.PI * 2.0f * index / 14.0f;
            var angle1 = Mathf.PI * 2.0f * (index + 0.42f) / 14.0f;
            var front = center + new Vector3(Mathf.Cos(angle0) * 0.575f, Mathf.Sin(angle0) * 0.535f, 0.285f);
            var back = center + new Vector3(Mathf.Cos(angle1) * 0.575f, Mathf.Sin(angle1) * 0.535f, -0.285f);
            builder.AddRod(front, back, 0.012f, 5, 4);
            var tabCenter = center + new Vector3(Mathf.Cos(angle0) * 0.595f, Mathf.Sin(angle0) * 0.555f, 0.285f);
            var radial = new Vector3(Mathf.Cos(angle0), Mathf.Sin(angle0), 0.0f);
            var tangent = new Vector3(-Mathf.Sin(angle0), Mathf.Cos(angle0), 0.0f);
            builder.AddOrientedBox(tabCenter, radial, tangent, Vector3.forward, new Vector3(0.042f, 0.090f, 0.036f), 4);
        }
    }

    private static void AddSideCrossBraces(MeshBuilder builder, Vector3 center)
    {
        for (var index = 0; index < 12; index++)
        {
            var angle0 = Mathf.PI * 2.0f * index / 12.0f;
            var angle1 = Mathf.PI * 2.0f * (index + 0.55f) / 12.0f;
            var angle2 = Mathf.PI * 2.0f * (index - 0.55f) / 12.0f;
            var front = center + new Vector3(Mathf.Cos(angle0) * 0.600f, Mathf.Sin(angle0) * 0.560f, 0.225f);
            var backA = center + new Vector3(Mathf.Cos(angle1) * 0.600f, Mathf.Sin(angle1) * 0.560f, -0.225f);
            var backB = center + new Vector3(Mathf.Cos(angle2) * 0.600f, Mathf.Sin(angle2) * 0.560f, -0.225f);
            builder.AddRod(front, backA, 0.010f, 5, 4);
            builder.AddRod(front, backB, 0.010f, 5, 4);
        }
    }

    private static void AddLaceKnots(MeshBuilder builder, Vector3 center)
    {
        for (var index = 0; index < 14; index++)
        {
            var angle = Mathf.PI * 2.0f * index / 14.0f;
            var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
            var tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);
            var frontKnot = center + new Vector3(radial.x * 0.600f, radial.y * 0.560f, 0.305f);
            var backKnot = center + new Vector3(radial.x * 0.600f, radial.y * 0.560f, -0.305f);
            builder.AddOrientedBox(frontKnot, radial, tangent, Vector3.forward, new Vector3(0.040f, 0.045f, 0.026f), 4);
            builder.AddOrientedBox(backKnot, radial, tangent, Vector3.forward, new Vector3(0.036f, 0.040f, 0.024f), 4);
        }
    }

    private static void AddExtraEdgeTabs(MeshBuilder builder, Vector3 center)
    {
        for (var index = 0; index < 24; index++)
        {
            var angle = Mathf.PI * 2.0f * (index + 0.25f) / 24.0f;
            var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
            var tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);
            var z = index % 2 == 0 ? 0.307f : -0.307f;
            var tabCenter = center + new Vector3(radial.x * 0.525f, radial.y * 0.495f, z);
            var tabLength = index % 3 == 0 ? 0.070f : 0.050f;
            builder.AddOrientedBox(tabCenter, radial, tangent, Vector3.forward, new Vector3(0.026f, tabLength, 0.020f), 4);
        }
    }

    private static void AddIrregularHidePatches(MeshBuilder builder, Vector3 center)
    {
        var z = center.z + 0.313f;
        var patches = new[]
        {
            new Vector3(-0.28f, -0.10f, z),
            new Vector3(-0.16f, 0.28f, z),
            new Vector3(0.23f, -0.23f, z),
            new Vector3(0.30f, 0.14f, z),
            new Vector3(-0.38f, 0.12f, z),
            new Vector3(0.03f, -0.35f, z)
        };

        for (var index = 0; index < patches.Length; index++)
        {
            var start = center + patches[index];
            var angle = 0.65f + index * 0.9f;
            var faceX = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
            var faceY = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0.0f);
            builder.AddOrientedBox(start, faceX, faceY, Vector3.forward, new Vector3(0.090f, 0.030f, 0.010f), index % 2 == 0 ? 3 : 2);
            var end = start + new Vector3(Mathf.Cos(angle) * 0.075f, Mathf.Sin(angle) * 0.050f, 0.0f);
            builder.AddRod(start, end, 0.008f, 4, index % 2 == 0 ? 3 : 2);
        }
    }

    private static void AddBroadHidePanelVariation(MeshBuilder builder, Vector3 center)
    {
        var z = center.z + 0.308f;
        var panels = new[]
        {
            (Position: new Vector3(-0.17f, 0.16f, z), Rotation: 0.35f, Size: new Vector3(0.18f, 0.055f, 0.009f), Material: 3),
            (Position: new Vector3(0.19f, -0.12f, z), Rotation: -0.55f, Size: new Vector3(0.20f, 0.050f, 0.009f), Material: 2),
            (Position: new Vector3(-0.23f, -0.20f, z), Rotation: -0.15f, Size: new Vector3(0.13f, 0.045f, 0.009f), Material: 3),
            (Position: new Vector3(0.25f, 0.21f, z), Rotation: 0.70f, Size: new Vector3(0.14f, 0.040f, 0.009f), Material: 2)
        };

        foreach (var panel in panels)
        {
            var axisX = new Vector3(Mathf.Cos(panel.Rotation), Mathf.Sin(panel.Rotation), 0.0f);
            var axisY = new Vector3(-Mathf.Sin(panel.Rotation), Mathf.Cos(panel.Rotation), 0.0f);
            builder.AddOrientedBox(center + panel.Position, axisX, axisY, Vector3.forward, panel.Size, panel.Material);
        }
    }


    private static void AddTopGrip(MeshBuilder builder, Vector3 center)
    {
        var leftAnchor = center + new Vector3(-0.22f, 0.34f, -0.04f);
        var rightAnchor = center + new Vector3(0.22f, 0.34f, -0.04f);
        var leftTop = new Vector3(-0.12f, -0.08f, -0.05f);
        var rightTop = new Vector3(0.12f, -0.08f, -0.05f);
        builder.AddRod(leftAnchor, leftTop, 0.026f, RodSegments, 4);
        builder.AddRod(rightAnchor, rightTop, 0.026f, RodSegments, 4);
        builder.AddRod(leftTop, rightTop, 0.040f, RodSegments, 4);
        builder.AddRod(new Vector3(-0.10f, -0.08f, -0.04f), new Vector3(0.10f, -0.08f, -0.04f), 0.055f, RodSegments, 3);
    }

    private static void AddSideHandles(MeshBuilder builder, Vector3 center)
    {
        AddSideHandle(builder, center, -1.0f);
        AddSideHandle(builder, center, 1.0f);
    }

    private static void AddSideHandle(MeshBuilder builder, Vector3 center, float side)
    {
        var sideAxis = new Vector3(side, 0.0f, 0.0f);
        var tangent = Vector3.up;
        var upperAnchor = center + new Vector3(side * 0.59f, 0.16f, -0.02f);
        var lowerAnchor = center + new Vector3(side * 0.59f, -0.16f, -0.02f);
        var outerUpper = center + new Vector3(side * 0.69f, 0.09f, -0.040f);
        var outerLower = center + new Vector3(side * 0.69f, -0.09f, -0.040f);
        var gripCenter = center + new Vector3(side * 0.71f, 0.0f, -0.045f);

        builder.AddOrientedBox(upperAnchor, sideAxis, tangent, Vector3.forward, new Vector3(0.055f, 0.085f, 0.045f), 4);
        builder.AddOrientedBox(lowerAnchor, sideAxis, tangent, Vector3.forward, new Vector3(0.055f, 0.085f, 0.045f), 4);
        builder.AddRod(upperAnchor + new Vector3(side * 0.020f, -0.020f, -0.005f), outerUpper, 0.020f, RodSegments, 4);
        builder.AddRod(outerUpper, outerLower, 0.026f, RodSegments, 4);
        builder.AddRod(outerLower, lowerAnchor + new Vector3(side * 0.020f, 0.020f, -0.005f), 0.020f, RodSegments, 4);
        builder.AddRod(gripCenter + new Vector3(0.0f, -0.070f, 0.006f), gripCenter + new Vector3(0.0f, 0.070f, 0.006f), 0.034f, RodSegments, 3);
    }

    private static void AddRaisedKnotworkPaint(MeshBuilder builder, Vector3 center)
    {
        var z = center.z + 0.320f;
        AddEllipseRing(builder, center, 0.32f, 0.30f, z - center.z, 0.009f, 5);
        builder.AddRod(center + new Vector3(-0.25f, 0.0f, z), center + new Vector3(0.25f, 0.0f, z), 0.012f, 4, 5);
        builder.AddRod(center + new Vector3(0.0f, -0.25f, z), center + new Vector3(0.0f, 0.27f, z), 0.012f, 4, 5);
        builder.AddRod(center + new Vector3(-0.30f, 0.22f, z), center + new Vector3(0.30f, 0.22f, z), 0.010f, 4, 5);
        builder.AddRod(center + new Vector3(-0.22f, -0.26f, z), center + new Vector3(0.22f, 0.24f, z), 0.010f, 4, 5);
        builder.AddRod(center + new Vector3(0.22f, -0.26f, z), center + new Vector3(-0.22f, 0.24f, z), 0.010f, 4, 5);
        AddKnotLoop(builder, center, z, 0.0f);
        AddKnotLoop(builder, center, z, Mathf.PI * 2.0f / 3.0f);
        AddKnotLoop(builder, center, z, Mathf.PI * 4.0f / 3.0f);
        for (var index = 0; index < 6; index++)
        {
            var angle = Mathf.PI * 2.0f * index / 6.0f + 0.35f;
            builder.AddRod(
                center + new Vector3(Mathf.Cos(angle) * 0.15f, Mathf.Sin(angle) * 0.14f, z),
                center + new Vector3(Mathf.Cos(angle) * 0.31f, Mathf.Sin(angle) * 0.29f, z),
                0.006f,
                4,
                5);
        }
    }

    private static void AddKnotLoop(MeshBuilder builder, Vector3 center, float z, float rotation)
    {
        var a = center + RotateFace(new Vector3(0.0f, 0.075f, z), rotation);
        var b = center + RotateFace(new Vector3(0.115f, -0.055f, z), rotation);
        var c = center + RotateFace(new Vector3(-0.115f, -0.055f, z), rotation);
        builder.AddRod(a, b, 0.009f, 4, 5);
        builder.AddRod(b, c, 0.009f, 4, 5);
        builder.AddRod(c, a, 0.009f, 4, 5);
    }

    private static Vector3 RotateFace(Vector3 point, float rotation)
    {
        return new Vector3(
            point.x * Mathf.Cos(rotation) - point.y * Mathf.Sin(rotation),
            point.x * Mathf.Sin(rotation) + point.y * Mathf.Cos(rotation),
            point.z);
    }

    private static void AddEllipseRing(MeshBuilder builder, Vector3 center, float radiusX, float radiusY, float zOffset, float radius, int material)
    {
        const int Segments = 24;
        for (var index = 0; index < Segments; index++)
        {
            var angle0 = Mathf.PI * 2.0f * index / Segments;
            var angle1 = Mathf.PI * 2.0f * (index + 1) / Segments;
            builder.AddRod(
                center + new Vector3(Mathf.Cos(angle0) * radiusX, Mathf.Sin(angle0) * radiusY, zOffset),
                center + new Vector3(Mathf.Cos(angle1) * radiusX, Mathf.Sin(angle1) * radiusY, zOffset),
                radius,
                4,
                material);
        }
    }

    private static Material[] CreateMaterials()
    {
        var shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
        return new[]
        {
            CreateMaterial(shader, "Bardheim_HangingDrumDarkOak", new Color(0.18f, 0.085f, 0.035f, 1.0f)),
            CreateMaterial(shader, "Bardheim_HangingDrumRawhide", new Color(0.58f, 0.46f, 0.29f, 1.0f)),
            CreateMaterial(shader, "Bardheim_HangingDrumWornBronze", new Color(0.40f, 0.29f, 0.13f, 1.0f)),
            CreateMaterial(shader, "Bardheim_HangingDrumOakHighlight", new Color(0.56f, 0.34f, 0.13f, 1.0f)),
            CreateMaterial(shader, "Bardheim_HangingDrumLeather", new Color(0.12f, 0.055f, 0.025f, 1.0f)),
            CreateMaterial(shader, "Bardheim_HangingDrumRedPaint", new Color(0.32f, 0.045f, 0.025f, 1.0f))
        };
    }

    private static Material CreateMaterial(Shader shader, string name, Color color)
    {
        return new Material(shader)
        {
            name = name,
            color = color
        };
    }

    private static string GetPath(Transform root, Transform current)
    {
        var segments = new Stack<string>();
        var cursor = current;

        while (cursor is not null)
        {
            segments.Push(cursor.name);
            if (cursor == root)
            {
                break;
            }

            cursor = cursor.parent;
        }

        return string.Join("/", segments.ToArray());
    }

    private sealed class RendererTarget
    {
        public RendererTarget(MeshRenderer renderer, MeshFilter? filter)
        {
            Renderer = renderer;
            Filter = filter;
        }

        public MeshRenderer Renderer { get; }

        public MeshFilter? Filter { get; }
    }

    private sealed class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new();
        private readonly List<int>[] _triangles;

        public MeshBuilder(int materialCount)
        {
            _triangles = Enumerable.Range(0, materialCount).Select(_ => new List<int>()).ToArray();
        }

        public void ApplyTo(Mesh mesh)
        {
            mesh.SetVertices(_vertices);
            mesh.subMeshCount = _triangles.Length;
            for (var index = 0; index < _triangles.Length; index++)
            {
                mesh.SetTriangles(_triangles[index].ToArray(), index);
            }
        }

        public void AddCylinderZ(Vector3 center, float radiusX, float radiusY, float depth, int segments, int materialSide, int materialCap)
        {
            var front = new int[segments];
            var back = new int[segments];
            for (var index = 0; index < segments; index++)
            {
                var angle = Mathf.PI * 2.0f * index / segments;
                var wobble = 1.0f + 0.025f * Mathf.Sin(index * 2.1f);
                var x = Mathf.Cos(angle) * radiusX * wobble;
                var y = Mathf.Sin(angle) * radiusY * wobble;
                front[index] = AddVertex(center + new Vector3(x, y, depth * 0.5f));
                back[index] = AddVertex(center + new Vector3(x, y, -depth * 0.5f));
            }

            for (var index = 0; index < segments; index++)
            {
                AddQuad(front[index], front[(index + 1) % segments], back[(index + 1) % segments], back[index], materialSide);
            }

            var frontCenter = AddVertex(center + new Vector3(0.0f, 0.0f, depth * 0.5f));
            var backCenter = AddVertex(center + new Vector3(0.0f, 0.0f, -depth * 0.5f));
            for (var index = 0; index < segments; index++)
            {
                AddTriangle(frontCenter, front[index], front[(index + 1) % segments], materialCap);
                AddTriangle(backCenter, back[(index + 1) % segments], back[index], materialCap);
            }
        }

        public void AddCylinderSideZ(Vector3 center, float radiusX, float radiusY, float depth, int segments, int materialSide)
        {
            var front = new int[segments];
            var back = new int[segments];
            for (var index = 0; index < segments; index++)
            {
                var angle = Mathf.PI * 2.0f * index / segments;
                var wobble = 1.0f + 0.018f * Mathf.Sin(index * 1.8f);
                var x = Mathf.Cos(angle) * radiusX * wobble;
                var y = Mathf.Sin(angle) * radiusY * wobble;
                front[index] = AddVertex(center + new Vector3(x, y, depth * 0.5f));
                back[index] = AddVertex(center + new Vector3(x, y, -depth * 0.5f));
            }

            for (var index = 0; index < segments; index++)
            {
                var next = (index + 1) % segments;
                AddQuad(front[index], front[next], back[next], back[index], materialSide);
                AddQuad(back[index], back[next], front[next], front[index], materialSide);
            }
        }

        public void AddOrientedBox(Vector3 center, Vector3 axisX, Vector3 axisY, Vector3 axisZ, Vector3 size, int material)
        {
            axisX.Normalize();
            axisY.Normalize();
            axisZ.Normalize();
            var half = size * 0.5f;
            var ids = new int[8];
            var cursor = 0;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        ids[cursor++] = AddVertex(center + axisX * half.x * x + axisY * half.y * y + axisZ * half.z * z);
                    }
                }
            }

            AddQuad(ids[0], ids[2], ids[3], ids[1], material);
            AddQuad(ids[4], ids[5], ids[7], ids[6], material);
            AddQuad(ids[0], ids[1], ids[5], ids[4], material);
            AddQuad(ids[2], ids[6], ids[7], ids[3], material);
            AddQuad(ids[1], ids[3], ids[7], ids[5], material);
            AddQuad(ids[0], ids[4], ids[6], ids[2], material);
        }

        public void AddRod(Vector3 start, Vector3 end, float radius, int segments, int material)
        {
            var axis = end - start;
            var forward = axis.normalized;
            var helper = Mathf.Abs(forward.z) < 0.92f ? Vector3.forward : Vector3.up;
            var right = Vector3.Cross(forward, helper).normalized;
            var up = Vector3.Cross(right, forward).normalized;
            var startRing = new int[segments];
            var endRing = new int[segments];

            for (var index = 0; index < segments; index++)
            {
                var angle = Mathf.PI * 2.0f * index / segments;
                var offset = right * Mathf.Cos(angle) * radius + up * Mathf.Sin(angle) * radius;
                startRing[index] = AddVertex(start + offset);
                endRing[index] = AddVertex(end + offset);
            }

            for (var index = 0; index < segments; index++)
            {
                AddQuad(startRing[index], startRing[(index + 1) % segments], endRing[(index + 1) % segments], endRing[index], material);
            }

            var startCenter = AddVertex(start);
            var endCenter = AddVertex(end);
            for (var index = 0; index < segments; index++)
            {
                AddTriangle(startCenter, startRing[(index + 1) % segments], startRing[index], material);
                AddTriangle(endCenter, endRing[index], endRing[(index + 1) % segments], material);
            }
        }

        private int AddVertex(Vector3 vertex)
        {
            _vertices.Add(vertex);
            return _vertices.Count - 1;
        }

        private void AddQuad(int a, int b, int c, int d, int material)
        {
            AddTriangle(a, b, c, material);
            AddTriangle(a, c, d, material);
        }

        private void AddTriangle(int a, int b, int c, int material)
        {
            _triangles[material].Add(a);
            _triangles[material].Add(b);
            _triangles[material].Add(c);
        }
    }
}
