using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Bardheim.Visuals;
using UnityEngine;

namespace Bardheim.Items;

internal static class LyreHammerVisualProbe
{
    private const string VisualName = "Bardheim_HammerCarrierLyreVisual";
    private const float VisualScale = 2.4f;
    private const int FrameSegmentCount = 14;
    private const int StringCount = 6;
    private const float StringBottomY = -0.29f;
    private const float StringTopY = 0.34f;
    private static readonly Vector3 GripOffset = new Vector3(0.50f, 0.0f, 0.0f);
    private static readonly Vector3 StringThickness = new Vector3(0.012f, 0.012f, 0.009f);

    public static void Apply(GameObject itemPrefab, ManualLogSource logger)
    {
        if (itemPrefab is null)
        {
            logger.LogWarning("Lyre Hammer-carrier visual probe skipped because the cloned item prefab is null.");
            return;
        }

        LogHierarchy(itemPrefab, logger);

        var lyreMesh = BuildLyreMesh();
        var materials = CreateMaterials();
        var renderers = itemPrefab.GetComponentsInChildren<Renderer>(true);
        var target = SelectTargetRenderer(itemPrefab);

        if (target is not null)
        {
            target.Filter!.sharedMesh = lyreMesh;
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
                $"Applied Hammer-carrier lyre visual by replacing renderer path={GetPath(itemPrefab.transform, target.Renderer.transform)} " +
                $"mesh={lyreMesh.name} materials={materials.Length} disabledRenderers={Math.Max(0, renderers.Length - 1)}.");
            return;
        }

        var fallback = new GameObject(VisualName);
        fallback.transform.SetParent(itemPrefab.transform, false);
        fallback.transform.localPosition = Vector3.zero;
        fallback.transform.localRotation = Quaternion.identity;
        fallback.transform.localScale = Vector3.one;

        var meshFilter = fallback.AddComponent<MeshFilter>();
        var meshRenderer = fallback.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = lyreMesh;
        meshRenderer.sharedMaterials = materials;
        EnsurePlayModePose(fallback);

        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }

        logger.LogWarning(
            $"Applied Hammer-carrier lyre visual as fallback child path={VisualName}; no MeshRenderer with MeshFilter was found to replace in place.");
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
        if (visual.GetComponent<LyrePlayModeVisualPose>() is null)
        {
            visual.AddComponent<LyrePlayModeVisualPose>();
        }
    }

    private static void LogHierarchy(GameObject itemPrefab, ManualLogSource logger)
    {
        var renderers = itemPrefab.GetComponentsInChildren<Renderer>(true);
        logger.LogInfo($"Hammer-carrier visual probe for {itemPrefab.name}: rendererCount={renderers.Length}.");

        foreach (var renderer in renderers)
        {
            var meshName = GetMeshName(renderer);
            var materialNames = string.Join(
                ",",
                renderer.sharedMaterials.Select(material => material is null ? "<null>" : material.name));

            logger.LogInfo(
                $"Hammer renderer path={GetPath(itemPrefab.transform, renderer.transform)} type={renderer.GetType().Name} " +
                $"enabled={renderer.enabled} mesh={meshName} materials=[{materialNames}].");
        }

        foreach (var component in itemPrefab.GetComponentsInChildren<Component>(true))
        {
            if (component is null)
            {
                continue;
            }

            var typeName = component.GetType().Name;
            if (typeName.Contains("Collider") || typeName == "Rigidbody")
            {
                logger.LogInfo($"Hammer carrier component path={GetPath(itemPrefab.transform, component.transform)} type={typeName}.");
            }

            if (component.transform.name.Equals("attach", StringComparison.OrdinalIgnoreCase) ||
                component.transform.name.Equals("equipoffset", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInfo($"Hammer held-offset child path={GetPath(itemPrefab.transform, component.transform)}.");
            }
        }
    }

    private static Mesh BuildLyreMesh()
    {
        var vertices = new List<Vector3>();
        var woodTriangles = new List<int>();
        var stringTriangles = new List<int>();

        AddRoundedFrame(vertices, woodTriangles);
        AddSoundboardPanel(vertices, woodTriangles);
        AddBridge(vertices, woodTriangles);
        AddDecorativeWoodwork(vertices, woodTriangles);

        for (var index = 0; index < StringCount; index++)
        {
            var t = StringCount == 1 ? 0.5f : index / (float)(StringCount - 1);
            var stringX = Mathf.Lerp(-0.16f, 0.16f, t);
            AddSlenderBoxBetween(
                vertices,
                stringTriangles,
                Scale(new Vector3(stringX, StringBottomY, -0.055f)),
                Scale(new Vector3(stringX, StringTopY, -0.055f)),
                Scale(StringThickness));
        }

        var mesh = new Mesh
        {
            name = "Bardheim_GeneratedHammerCarrierLyre"
        };
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(woodTriangles.ToArray(), 0);
        mesh.SetTriangles(stringTriangles.ToArray(), 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddRoundedFrame(ICollection<Vector3> vertices, ICollection<int> triangles)
    {
        var previousLeft = new Vector3(-0.27f, -0.31f, 0.0f);
        var previousRight = new Vector3(0.27f, -0.31f, 0.0f);

        for (var index = 1; index <= FrameSegmentCount; index++)
        {
            var t = index / (float)FrameSegmentCount;
            var y = Mathf.Lerp(-0.31f, 0.36f, t);
            var curve = Mathf.Sin(t * Mathf.PI) * 0.078f;
            var taper = Mathf.Lerp(0.27f, 0.215f, t);
            var currentLeft = new Vector3(-taper - curve, y, 0.0f);
            var currentRight = new Vector3(taper + curve, y, 0.0f);

            AddSlenderBoxBetween(vertices, triangles, Scale(previousLeft), Scale(currentLeft), Scale(new Vector3(0.078f, 0.078f, 0.075f)));
            AddSlenderBoxBetween(vertices, triangles, Scale(previousRight), Scale(currentRight), Scale(new Vector3(0.078f, 0.078f, 0.075f)));

            previousLeft = currentLeft;
            previousRight = currentRight;
        }

        AddSlenderBoxBetween(vertices, triangles, Scale(previousLeft), Scale(previousRight), Scale(new Vector3(0.09f, 0.09f, 0.075f)));
        AddBox(vertices, triangles, Scale(new Vector3(0.00f, -0.37f, 0.0f)), Scale(new Vector3(0.56f, 0.13f, 0.09f)));
        AddRoundedCornerCaps(vertices, triangles);

        for (var index = 0; index < 6; index++)
        {
            var x = -0.20f + index * 0.08f;
            AddTuningPeg(vertices, triangles, new Vector3(x, 0.43f, 0.0f));
        }
    }

    private static void AddSoundboardPanel(ICollection<Vector3> vertices, ICollection<int> triangles)
    {
        AddBox(vertices, triangles, Scale(new Vector3(0.0f, -0.29f, 0.018f)), Scale(new Vector3(0.40f, 0.16f, 0.035f)));
        AddBox(vertices, triangles, Scale(new Vector3(0.0f, -0.30f, -0.006f)), Scale(new Vector3(0.25f, 0.09f, 0.035f)));
    }

    private static void AddBridge(ICollection<Vector3> vertices, ICollection<int> triangles)
    {
        AddBox(vertices, triangles, Scale(new Vector3(0.0f, -0.18f, -0.060f)), Scale(new Vector3(0.38f, 0.035f, 0.04f)));
        AddBox(vertices, triangles, Scale(new Vector3(0.0f, 0.31f, -0.060f)), Scale(new Vector3(0.46f, 0.028f, 0.035f)));
    }

    private static void AddDecorativeWoodwork(ICollection<Vector3> vertices, ICollection<int> triangles)
    {
        AddCarvedBands(vertices, triangles);
        AddVikingKnotwork(vertices, triangles);

        AddBox(vertices, triangles, Scale(new Vector3(-0.315f, -0.05f, -0.072f)), Scale(new Vector3(0.026f, 0.30f, 0.018f)));
        AddBox(vertices, triangles, Scale(new Vector3(0.315f, -0.05f, -0.072f)), Scale(new Vector3(0.026f, 0.30f, 0.018f)));
        AddBox(vertices, triangles, Scale(new Vector3(-0.235f, -0.39f, -0.070f)), Scale(new Vector3(0.055f, 0.045f, 0.02f)));
        AddBox(vertices, triangles, Scale(new Vector3(0.235f, -0.39f, -0.070f)), Scale(new Vector3(0.055f, 0.045f, 0.02f)));
    }

    private static void AddRoundedCornerCaps(ICollection<Vector3> vertices, ICollection<int> triangles)
    {
        AddBox(vertices, triangles, Scale(new Vector3(-0.275f, -0.37f, 0.0f)), Scale(new Vector3(0.11f, 0.12f, 0.09f)));
        AddBox(vertices, triangles, Scale(new Vector3(0.275f, -0.37f, 0.0f)), Scale(new Vector3(0.11f, 0.12f, 0.09f)));
        AddBox(vertices, triangles, Scale(new Vector3(-0.225f, 0.365f, 0.0f)), Scale(new Vector3(0.10f, 0.10f, 0.08f)));
        AddBox(vertices, triangles, Scale(new Vector3(0.225f, 0.365f, 0.0f)), Scale(new Vector3(0.10f, 0.10f, 0.08f)));
    }

    private static void AddCarvedBands(ICollection<Vector3> vertices, ICollection<int> triangles)
    {
        AddBox(vertices, triangles, Scale(new Vector3(0.0f, -0.405f, -0.080f)), Scale(new Vector3(0.55f, 0.025f, 0.02f)));
        AddBox(vertices, triangles, Scale(new Vector3(0.0f, -0.335f, -0.080f)), Scale(new Vector3(0.48f, 0.018f, 0.018f)));
        AddBox(vertices, triangles, Scale(new Vector3(0.0f, 0.385f, -0.080f)), Scale(new Vector3(0.46f, 0.020f, 0.018f)));
    }

    private static void AddVikingKnotwork(ICollection<Vector3> vertices, ICollection<int> triangles)
    {
        for (var index = 0; index < 4; index++)
        {
            var y = -0.23f + index * 0.13f;
            AddOrnamentalCross(vertices, triangles, new Vector3(-0.315f, y, -0.086f), 34.0f);
            AddOrnamentalCross(vertices, triangles, new Vector3(0.315f, y, -0.086f), -34.0f);
        }

        AddOrientedBox(vertices, triangles, new Vector3(-0.10f, -0.405f, -0.092f), new Vector3(0.018f, 0.12f, 0.018f), 55.0f);
        AddOrientedBox(vertices, triangles, new Vector3(-0.10f, -0.405f, -0.091f), new Vector3(0.018f, 0.12f, 0.018f), -55.0f);
        AddOrientedBox(vertices, triangles, new Vector3(0.10f, -0.405f, -0.092f), new Vector3(0.018f, 0.12f, 0.018f), 55.0f);
        AddOrientedBox(vertices, triangles, new Vector3(0.10f, -0.405f, -0.091f), new Vector3(0.018f, 0.12f, 0.018f), -55.0f);
        AddOrientedBox(vertices, triangles, new Vector3(0.0f, 0.385f, -0.092f), new Vector3(0.016f, 0.16f, 0.018f), 62.0f);
        AddOrientedBox(vertices, triangles, new Vector3(0.0f, 0.385f, -0.091f), new Vector3(0.016f, 0.16f, 0.018f), -62.0f);
    }

    private static void AddOrnamentalCross(ICollection<Vector3> vertices, ICollection<int> triangles, Vector3 localCenter, float rotationDegrees)
    {
        AddOrientedBox(vertices, triangles, localCenter, new Vector3(0.014f, 0.085f, 0.016f), rotationDegrees);
        AddOrientedBox(vertices, triangles, localCenter + new Vector3(0.0f, 0.0f, 0.001f), new Vector3(0.014f, 0.085f, 0.016f), -rotationDegrees);
    }

    private static void AddTuningPeg(ICollection<Vector3> vertices, ICollection<int> triangles, Vector3 localCenter)
    {
        AddBox(vertices, triangles, Scale(localCenter), Scale(new Vector3(0.035f, 0.09f, 0.05f)));
        AddBox(vertices, triangles, Scale(localCenter + new Vector3(0.0f, 0.045f, -0.025f)), Scale(new Vector3(0.055f, 0.025f, 0.035f)));
    }

    private static void AddSlenderBoxBetween(
        ICollection<Vector3> vertices,
        ICollection<int> triangles,
        Vector3 start,
        Vector3 end,
        Vector3 thickness)
    {
        var center = (start + end) * 0.5f;
        var delta = end - start;
        var size = new Vector3(
            Mathf.Max(thickness.x, Mathf.Abs(delta.x) + thickness.x),
            Mathf.Max(thickness.y, Mathf.Abs(delta.y) + thickness.y),
            thickness.z);

        AddBox(vertices, triangles, center, size);
    }

    private static Vector3 Scale(Vector3 value)
    {
        return value * VisualScale;
    }

    private static Vector3 ApplyGripOffset(Vector3 value)
    {
        return value + GripOffset;
    }

    private static Material[] CreateMaterials()
    {
        var shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
        var wood = new Material(shader)
        {
            name = "Bardheim_WarmWood",
            color = new Color(0.48f, 0.27f, 0.12f, 1.0f)
        };
        var strings = new Material(shader)
        {
            name = "Bardheim_PaleStrings",
            color = new Color(0.82f, 0.72f, 0.52f, 1.0f)
        };

        return new Material[2] { wood, strings };
    }

    private static void AddBox(ICollection<Vector3> vertices, ICollection<int> triangles, Vector3 center, Vector3 size)
    {
        var start = vertices.Count;
        var half = size * 0.5f;

        vertices.Add(ApplyGripOffset(center + new Vector3(-half.x, -half.y, -half.z)));
        vertices.Add(ApplyGripOffset(center + new Vector3(half.x, -half.y, -half.z)));
        vertices.Add(ApplyGripOffset(center + new Vector3(half.x, half.y, -half.z)));
        vertices.Add(ApplyGripOffset(center + new Vector3(-half.x, half.y, -half.z)));
        vertices.Add(ApplyGripOffset(center + new Vector3(-half.x, -half.y, half.z)));
        vertices.Add(ApplyGripOffset(center + new Vector3(half.x, -half.y, half.z)));
        vertices.Add(ApplyGripOffset(center + new Vector3(half.x, half.y, half.z)));
        vertices.Add(ApplyGripOffset(center + new Vector3(-half.x, half.y, half.z)));

        AddFace(triangles, start, 0, 2, 1, 0, 3, 2);
        AddFace(triangles, start, 4, 5, 6, 4, 6, 7);
        AddFace(triangles, start, 0, 1, 5, 0, 5, 4);
        AddFace(triangles, start, 2, 3, 7, 2, 7, 6);
        AddFace(triangles, start, 1, 2, 6, 1, 6, 5);
        AddFace(triangles, start, 3, 0, 4, 3, 4, 7);
    }

    private static void AddOrientedBox(
        ICollection<Vector3> vertices,
        ICollection<int> triangles,
        Vector3 localCenter,
        Vector3 localSize,
        float rotationDegrees)
    {
        var start = vertices.Count;
        var half = localSize * 0.5f;
        var radians = rotationDegrees * Mathf.Deg2Rad;
        var sin = Mathf.Sin(radians);
        var cos = Mathf.Cos(radians);

        AddRotatedVertex(vertices, localCenter, new Vector3(-half.x, -half.y, -half.z), sin, cos);
        AddRotatedVertex(vertices, localCenter, new Vector3(half.x, -half.y, -half.z), sin, cos);
        AddRotatedVertex(vertices, localCenter, new Vector3(half.x, half.y, -half.z), sin, cos);
        AddRotatedVertex(vertices, localCenter, new Vector3(-half.x, half.y, -half.z), sin, cos);
        AddRotatedVertex(vertices, localCenter, new Vector3(-half.x, -half.y, half.z), sin, cos);
        AddRotatedVertex(vertices, localCenter, new Vector3(half.x, -half.y, half.z), sin, cos);
        AddRotatedVertex(vertices, localCenter, new Vector3(half.x, half.y, half.z), sin, cos);
        AddRotatedVertex(vertices, localCenter, new Vector3(-half.x, half.y, half.z), sin, cos);

        AddFace(triangles, start, 0, 2, 1, 0, 3, 2);
        AddFace(triangles, start, 4, 5, 6, 4, 6, 7);
        AddFace(triangles, start, 0, 1, 5, 0, 5, 4);
        AddFace(triangles, start, 2, 3, 7, 2, 7, 6);
        AddFace(triangles, start, 1, 2, 6, 1, 6, 5);
        AddFace(triangles, start, 3, 0, 4, 3, 4, 7);
    }

    private static void AddRotatedVertex(ICollection<Vector3> vertices, Vector3 localCenter, Vector3 localOffset, float sin, float cos)
    {
        var rotated = new Vector3(
            localOffset.x * cos - localOffset.y * sin,
            localOffset.x * sin + localOffset.y * cos,
            localOffset.z);

        vertices.Add(ApplyGripOffset(Scale(localCenter + rotated)));
    }

    private static void AddFace(ICollection<int> triangles, int start, params int[] indices)
    {
        foreach (var index in indices)
        {
            triangles.Add(start + index);
        }
    }

    private static string GetMeshName(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            return skinnedMeshRenderer.sharedMesh is null ? "<null>" : skinnedMeshRenderer.sharedMesh.name;
        }

        var meshFilter = renderer.GetComponent<MeshFilter>();
        return meshFilter?.sharedMesh is null ? "<null>" : meshFilter.sharedMesh.name;
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
}
