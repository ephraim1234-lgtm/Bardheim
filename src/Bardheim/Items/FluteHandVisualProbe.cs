using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Bardheim.Visuals;
using UnityEngine;

namespace Bardheim.Items;

internal static class FluteHandVisualProbe
{
    private const string VisualName = "Bardheim_OneHandedFluteVisual";
    private const int BodySegments = 16;
    private const int RingSegments = 12;

    public static void Apply(GameObject itemPrefab, ManualLogSource logger)
    {
        if (itemPrefab is null)
        {
            logger.LogWarning("Flute one-handed visual probe skipped because the cloned item prefab is null.");
            return;
        }

        var mesh = BuildFluteMesh();
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
                $"Applied one-handed flute visual by replacing renderer path={GetPath(itemPrefab.transform, target.Renderer.transform)} " +
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
            $"Applied one-handed flute visual as fallback child path={VisualName}; no MeshRenderer with MeshFilter was found to replace in place.");
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
        if (visual.GetComponent<FlutePlayModeVisualPose>() is null)
        {
            visual.AddComponent<FlutePlayModeVisualPose>();
        }
    }

    private static Mesh BuildFluteMesh()
    {
        var builder = new MeshBuilder(materialCount: 7);
        var start = new Vector3(-0.56f, -0.18f, 0.0f);
        var end = new Vector3(0.60f, -0.18f, 0.0f);

        AddTaperedBody(builder, start, end);
        AddEndCaps(builder, start, end);
        AddCarvedBands(builder);
        AddMetalInlayBands(builder);
        AddRaisedMouthpiece(builder);
        AddFingerHoles(builder);
        AddInsetToneHoleRims(builder);
        AddMouthNotch(builder);
        AddRuneCuts(builder);
        AddLeatherWrap(builder);

        var mesh = new Mesh
        {
            name = "Bardheim_GeneratedOneHandedFlute"
        };
        builder.ApplyTo(mesh);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddTaperedBody(MeshBuilder builder, Vector3 start, Vector3 end)
    {
        builder.AddTaperedRod(start, end, startRadius: 0.070f, endRadius: 0.048f, BodySegments, 0);
        builder.AddOrientedBox(
            new Vector3(0.02f, -0.126f, -0.050f),
            Vector3.right,
            Vector3.up,
            Vector3.forward,
            new Vector3(0.86f, 0.015f, 0.014f),
            5);
        builder.AddOrientedBox(
            new Vector3(0.02f, -0.236f, 0.046f),
            Vector3.right,
            Vector3.up,
            Vector3.forward,
            new Vector3(0.78f, 0.012f, 0.012f),
            3);
    }

    private static void AddEndCaps(MeshBuilder builder, Vector3 start, Vector3 end)
    {
        builder.AddRod(start + new Vector3(-0.032f, 0.0f, 0.0f), start + new Vector3(0.030f, 0.0f, 0.0f), 0.079f, RingSegments, 2);
        builder.AddRod(end + new Vector3(-0.032f, 0.0f, 0.0f), end + new Vector3(0.022f, 0.0f, 0.0f), 0.059f, RingSegments, 2);
        builder.AddRod(start + new Vector3(-0.052f, 0.0f, 0.0f), start + new Vector3(-0.034f, 0.0f, 0.0f), 0.056f, RingSegments, 6);
        builder.AddRod(end + new Vector3(0.023f, 0.0f, 0.0f), end + new Vector3(0.052f, 0.0f, 0.0f), 0.043f, RingSegments, 6);
    }

    private static void AddFingerHoles(MeshBuilder builder)
    {
        for (var index = 0; index < 6; index++)
        {
            var x = -0.18f + index * 0.115f;
            builder.AddOrientedBox(
                new Vector3(x, -0.125f, -0.020f),
                Vector3.right,
                Vector3.forward,
                Vector3.up,
                new Vector3(0.034f, 0.026f, 0.014f),
                1);
        }
    }

    private static void AddInsetToneHoleRims(MeshBuilder builder)
    {
        for (var index = 0; index < 6; index++)
        {
            var x = -0.18f + index * 0.115f;
            builder.AddOrientedBox(
                new Vector3(x, -0.121f, -0.025f),
                Vector3.right,
                Vector3.forward,
                Vector3.up,
                new Vector3(0.048f, 0.010f, 0.012f),
                index % 2 == 0 ? 2 : 5);
            builder.AddOrientedBox(
                new Vector3(x, -0.121f, -0.001f),
                Vector3.right,
                Vector3.forward,
                Vector3.up,
                new Vector3(0.048f, 0.010f, 0.012f),
                3);
        }
    }

    private static void AddRaisedMouthpiece(MeshBuilder builder)
    {
        var center = new Vector3(-0.505f, -0.180f, -0.020f);
        builder.AddRod(center + new Vector3(-0.050f, 0.0f, 0.0f), center + new Vector3(0.070f, 0.0f, 0.0f), 0.083f, RingSegments, 5);
        builder.AddRod(center + new Vector3(-0.028f, 0.0f, -0.010f), center + new Vector3(0.045f, 0.0f, -0.010f), 0.058f, RingSegments, 0);
        builder.AddOrientedBox(
            new Vector3(-0.505f, -0.116f, -0.026f),
            Vector3.right,
            Vector3.forward,
            Vector3.up,
            new Vector3(0.110f, 0.042f, 0.018f),
            6);
    }

    private static void AddMouthNotch(MeshBuilder builder)
    {
        builder.AddOrientedBox(
            new Vector3(-0.505f, -0.124f, -0.012f),
            Vector3.right,
            Vector3.forward,
            Vector3.up,
            new Vector3(0.070f, 0.030f, 0.018f),
            1);
        builder.AddOrientedBox(
            new Vector3(-0.560f, -0.180f, -0.058f),
            Vector3.right,
            Vector3.up,
            Vector3.forward,
            new Vector3(0.055f, 0.018f, 0.018f),
            3);
    }

    private static void AddCarvedBands(MeshBuilder builder)
    {
        var bandXs = new[] { -0.42f, -0.31f, 0.32f, 0.45f };
        foreach (var x in bandXs)
        {
            builder.AddRod(new Vector3(x - 0.010f, -0.18f, 0.0f), new Vector3(x + 0.010f, -0.18f, 0.0f), 0.071f, RingSegments, 3);
        }
    }

    private static void AddMetalInlayBands(MeshBuilder builder)
    {
        var bandXs = new[] { -0.380f, -0.270f, 0.270f, 0.400f };
        foreach (var x in bandXs)
        {
            builder.AddRod(new Vector3(x - 0.006f, -0.18f, 0.0f), new Vector3(x + 0.006f, -0.18f, 0.0f), 0.074f, RingSegments, 2);
        }

        builder.AddOrientedBox(new Vector3(-0.055f, -0.118f, -0.033f), Vector3.right, Vector3.forward, Vector3.up, new Vector3(0.070f, 0.012f, 0.012f), 2);
        builder.AddOrientedBox(new Vector3(0.135f, -0.118f, -0.033f), Vector3.right, Vector3.forward, Vector3.up, new Vector3(0.070f, 0.012f, 0.012f), 2);
    }

    private static void AddRuneCuts(MeshBuilder builder)
    {
        builder.AddOrientedBox(new Vector3(-0.285f, -0.118f, -0.016f), Vector3.right, Vector3.forward, Vector3.up, new Vector3(0.013f, 0.080f, 0.011f), 3);
        builder.AddOrientedBox(new Vector3(-0.268f, -0.116f, -0.016f), Vector3.right, Vector3.forward, Vector3.up, new Vector3(0.013f, 0.050f, 0.011f), 3);
        builder.AddOrientedBox(new Vector3(0.215f, -0.118f, -0.016f), Vector3.right, Vector3.forward, Vector3.up, new Vector3(0.013f, 0.075f, 0.011f), 3);
        builder.AddOrientedBox(new Vector3(0.238f, -0.115f, -0.016f), Vector3.right, Vector3.forward, Vector3.up, new Vector3(0.013f, 0.048f, 0.011f), 3);
    }

    private static void AddLeatherWrap(MeshBuilder builder)
    {
        builder.AddOrientedBox(new Vector3(-0.395f, -0.180f, -0.068f), Vector3.right, Vector3.up, Vector3.forward, new Vector3(0.045f, 0.110f, 0.020f), 4);
        builder.AddOrientedBox(new Vector3(0.455f, -0.180f, -0.068f), Vector3.right, Vector3.up, Vector3.forward, new Vector3(0.045f, 0.110f, 0.020f), 4);
    }

    private static Material[] CreateMaterials()
    {
        var shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
        return new[]
        {
            CreateMaterial(shader, "Bardheim_FlutePolishedBirch", new Color(0.62f, 0.36f, 0.16f, 1.0f)),
            CreateMaterial(shader, "Bardheim_FluteDarkHoles", new Color(0.025f, 0.018f, 0.012f, 1.0f)),
            CreateMaterial(shader, "Bardheim_FluteWornBronzeInlay", new Color(0.54f, 0.39f, 0.16f, 1.0f)),
            CreateMaterial(shader, "Bardheim_FluteCarvedDark", new Color(0.16f, 0.07f, 0.03f, 1.0f)),
            CreateMaterial(shader, "Bardheim_FluteLeatherWrap", new Color(0.13f, 0.055f, 0.025f, 1.0f)),
            CreateMaterial(shader, "Bardheim_FluteHoneyWoodHighlight", new Color(0.86f, 0.58f, 0.25f, 1.0f)),
            CreateMaterial(shader, "Bardheim_FluteBoneMouthpiece", new Color(0.72f, 0.58f, 0.38f, 1.0f))
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
            AddTaperedRod(start, end, radius, radius, segments, material);
        }

        public void AddTaperedRod(Vector3 start, Vector3 end, float startRadius, float endRadius, int segments, int material)
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
                startRing[index] = AddVertex(start + right * Mathf.Cos(angle) * startRadius + up * Mathf.Sin(angle) * startRadius);
                endRing[index] = AddVertex(end + right * Mathf.Cos(angle) * endRadius + up * Mathf.Sin(angle) * endRadius);
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
