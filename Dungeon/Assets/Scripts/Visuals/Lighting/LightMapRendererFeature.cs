using System.Collections.Generic;
using Dungeon.Logic.Services;
using Dungeon.Visuals;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Dungeon.Visuals.Lighting
{
    // URP ScriptableRendererFeature that renders all light sources into a
    // screen-sized binary light map (R8_UNorm). The result is exposed as global shader
    // texture _LightMap so any material can sample it for custom 2D lighting.
    //
    // Supports two light types:
    //
    //   GLOBAL DIRECTIONAL LIGHT
    //   ────────────────────────
    //   Fills the entire light map with white (lit), then subtracts directional
    //   shadow geometry behind shadow casters.  Shadow direction is opposite to
    //   the global light direction.  Shadow length = MaxShadowLength × Intensity.
    //
    //   POINT LIGHTS
    //   ────────────
    //   Each LightSource is a hard-edged circle drawn with additive blending.
    //   Per-light radial shadows are subtracted using BlendOp Min.
    //
    // Rendering order: global light → global shadows → point lights → point shadows.
    // Point lights can illuminate areas that are in global shadow.
    public class LightMapRendererFeature : ScriptableRendererFeature
    {
        private const string LIGHT_SHADER_NAME = "Dungeon/LightCircle";
        private const string SHADOW_SHADER_NAME = "Hidden/Dungeon/ShadowGeometry";
        private const string GLOBAL_LIGHT_SHADER_NAME = "Dungeon/GlobalLight";

        private LightMapPass _pass;
        private Material _lightCircleMaterial;
        private Material _shadowMaterial;
        private Material _globalLightMaterial;

        public override void Create()
        {
            Shader lightShader = Shader.Find(LIGHT_SHADER_NAME);
            if (lightShader != null)
            {
                _lightCircleMaterial = CoreUtils.CreateEngineMaterial(lightShader);
            }

            Shader shadowShader = Shader.Find(SHADOW_SHADER_NAME);
            if (shadowShader != null)
            {
                _shadowMaterial = CoreUtils.CreateEngineMaterial(shadowShader);
            }

            Shader globalLightShader = Shader.Find(GLOBAL_LIGHT_SHADER_NAME);
            if (globalLightShader != null)
            {
                _globalLightMaterial = CoreUtils.CreateEngineMaterial(globalLightShader);
            }

            _pass = new LightMapPass(_lightCircleMaterial, _shadowMaterial, _globalLightMaterial);
            _pass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Skip preview and reflection cameras.
            if (renderingData.cameraData.cameraType == CameraType.Preview ||
                renderingData.cameraData.cameraType == CameraType.Reflection)
            {
                return;
            }

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_lightCircleMaterial);
            _lightCircleMaterial = null;
            CoreUtils.Destroy(_shadowMaterial);
            _shadowMaterial = null;
            CoreUtils.Destroy(_globalLightMaterial);
            _globalLightMaterial = null;
        }

        // ── Inner render pass ──────────────────────────────────────────────────

        private class LightMapPass : ScriptableRenderPass
        {
            private static readonly int LIGHT_MAP_ID = Shader.PropertyToID("_LightMap");
            private static readonly int LIGHT_MAP_PARAMS_ID = Shader.PropertyToID("_LightMapParams");
            private const float POINT_SHADOW_EXTRUDE_DISTANCE = 100f;

            private readonly Material _lightMaterial;
            private readonly Material _shadowMaterial;
            private readonly Material _globalLightMaterial;
            private readonly Mesh _quadMesh;
            // Shadow meshes from the previous frame — destroyed at the start of the next frame to avoid leaks.
            private readonly List<Mesh> _previousShadowMeshes = new List<Mesh>();

            internal LightMapPass(Material lightMaterial, Material shadowMaterial, Material globalLightMaterial)
            {
                profilingSampler = new ProfilingSampler("LightMap Pass");
                _lightMaterial = lightMaterial;
                _shadowMaterial = shadowMaterial;
                _globalLightMaterial = globalLightMaterial;
                _quadMesh = CreateQuadMesh();
            }

            // ── Per-light shadow data ─────────────────────────────────────
            private struct PointLightData
            {
                internal Vector4 QuadParams; // NDC position and radius for the light circle
                internal Mesh ShadowMesh;    // Combined shadow geometry mesh for this light (null if none)
            }

            // ── Pass data for RenderGraph ──────────────────────────────────
            private class PassData
            {
                internal Material LightMaterial;
                internal Material ShadowMaterial;
                internal Material GlobalLightMaterial;
                internal Mesh QuadMesh;
                internal TextureHandle LightMapTexture;
                internal int ScreenWidth;
                internal int ScreenHeight;

                // Global directional light.
                internal bool HasGlobalLight;
                internal Mesh GlobalShadowMesh;

                // Point lights.
                internal bool HasPointLights;
                internal List<PointLightData> PointLightEntries;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // Destroy shadow meshes from the previous frame to prevent leaks.
                foreach (Mesh oldMesh in _previousShadowMeshes)
                {
                    Object.DestroyImmediate(oldMesh);
                }
                _previousShadowMeshes.Clear();

                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                int screenWidth = cameraData.cameraTargetDescriptor.width;
                int screenHeight = cameraData.cameraTargetDescriptor.height;

                // Create the light map render texture (single-channel, screen-sized).
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.depthStencilFormat = GraphicsFormat.None;
                desc.msaaSamples = 1;
                desc.graphicsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, GraphicsFormatUsage.Blend)
                    ? GraphicsFormat.R8_UNorm
                    : GraphicsFormat.B8G8R8A8_UNorm;

                TextureHandle lightMapTex = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, desc, "_LightMapRT", true);

                Camera cam = cameraData.camera;

                // ── Read global light state from Logic via GameBootstrapper ───
                GameBootstrapper bootstrapper = GameBootstrapper.Instance;
                LightingService lighting = null;
                if (bootstrapper != null && bootstrapper.LogicWorld != null)
                {
                    bootstrapper.LogicWorld.TryGet(out lighting);
                }
                bool hasGlobalLight = lighting != null && lighting.GlobalIntensity > 0f;
                float globalIntensity = hasGlobalLight ? lighting.GlobalIntensity : 0f;
                Vector2 globalLightDir = hasGlobalLight ? lighting.GlobalLightDirection : Vector2.zero;

                // ── Gather shadow casters from Logic ─────────────────────────
                List<ShadowCasterRenderData> shadowCasterData = new List<ShadowCasterRenderData>();
                if (lighting != null)
                {
                    lighting.GetShadowCasterData(shadowCasterData);
                }

                // ── Build global directional shadow mesh ─────────────────────
                Mesh globalShadowMesh = null;
                if (hasGlobalLight)
                {
                    globalShadowMesh = BuildDirectionalShadowMesh(globalLightDir, globalIntensity, shadowCasterData, cam, screenWidth, screenHeight);
                }

                // ── Gather point lights from Logic ───────────────────────────
                List<PointLightRenderData> pointLightRenderData = new List<PointLightRenderData>();
                if (lighting != null)
                {
                    lighting.GetPointLightData(pointLightRenderData);
                }
                List<PointLightData> pointLightEntries = new List<PointLightData>();
                bool hasPointLights = false;

                foreach (PointLightRenderData lightData in pointLightRenderData)
                {
                    hasPointLights = true;

                    Vector2 lightXZ = lightData.Position;
                    Vector3 lightWorldPos = new Vector3(lightXZ.x, 0f, lightXZ.y);

                    // Project light centre to screen space.
                    Vector3 screenPos = cam.WorldToScreenPoint(lightWorldPos);

                    // Project a point at the edge of the light radius to determine screen-space radius.
                    Vector3 camRight = cam.transform.right;
                    Vector3 camRightXZ = new Vector3(camRight.x, 0f, camRight.z).normalized;
                    Vector3 edgeWorldPos = lightWorldPos + camRightXZ * lightData.Radius;
                    Vector3 edgeScreenPos = cam.WorldToScreenPoint(edgeWorldPos);
                    float screenRadius = Vector2.Distance(
                        new Vector2(screenPos.x, screenPos.y),
                        new Vector2(edgeScreenPos.x, edgeScreenPos.y));

                    // Convert to NDC [-1, 1].
                    float ndcX = (screenPos.x / screenWidth) * 2f - 1f;
                    float ndcY = (screenPos.y / screenHeight) * 2f - 1f;
                    float ndcRadiusX = (screenRadius / screenWidth) * 2f;
                    float ndcRadiusY = (screenRadius / screenHeight) * 2f;

                    // Build radial shadow mesh for this point light.
                    Mesh shadowMesh = BuildRadialShadowMesh(lightXZ, lightData.Radius, shadowCasterData, cam, screenWidth, screenHeight);

                    PointLightData entry = new PointLightData
                    {
                        QuadParams = new Vector4(ndcX, ndcY, ndcRadiusX, ndcRadiusY),
                        ShadowMesh = shadowMesh
                    };
                    pointLightEntries.Add(entry);
                }

                // Track shadow meshes for cleanup next frame.
                if (globalShadowMesh != null)
                {
                    _previousShadowMeshes.Add(globalShadowMesh);
                }
                foreach (PointLightData entry in pointLightEntries)
                {
                    if (entry.ShadowMesh != null)
                    {
                        _previousShadowMeshes.Add(entry.ShadowMesh);
                    }
                }

                using (var builder = renderGraph.AddUnsafePass<PassData>(
                    "LightMap Render", out PassData passData, profilingSampler))
                {
                    passData.LightMaterial = _lightMaterial;
                    passData.ShadowMaterial = _shadowMaterial;
                    passData.GlobalLightMaterial = _globalLightMaterial;
                    passData.QuadMesh = _quadMesh;
                    passData.LightMapTexture = lightMapTex;
                    passData.ScreenWidth = screenWidth;
                    passData.ScreenHeight = screenHeight;
                    passData.HasGlobalLight = hasGlobalLight;
                    passData.GlobalShadowMesh = globalShadowMesh;
                    passData.HasPointLights = hasPointLights;
                    passData.PointLightEntries = pointLightEntries;

                    builder.UseTexture(lightMapTex, AccessFlags.WriteAll);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);

                    // Set _LightMap as a global texture after this pass completes.
                    builder.SetGlobalTextureAfterPass(lightMapTex, LIGHT_MAP_ID);

                    builder.SetRenderFunc(static (PassData data, UnsafeGraphContext context) =>
                    {
                        UnsafeCommandBuffer cmd = context.cmd;

                        // Set the light map as the render target.
                        cmd.SetRenderTarget(data.LightMapTexture);
                        cmd.SetViewport(new Rect(0, 0, data.ScreenWidth, data.ScreenHeight));

                        bool anyLight = data.HasGlobalLight || data.HasPointLights;

                        if (!anyLight)
                        {
                            // No lights at all: clear to white (fully lit fallback).
                            cmd.ClearRenderTarget(false, true, Color.white);
                        }
                        else
                        {
                            cmd.ClearRenderTarget(false, true, Color.black);

                            // Identity view/projection: we work in NDC directly.
                            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

                            // ── Global directional light ─────────────────────
                            if (data.HasGlobalLight && data.GlobalLightMaterial != null)
                            {
                                // Draw fullscreen white quad (global illumination).
                                Matrix4x4 fullscreenMatrix = Matrix4x4.TRS(
                                    Vector3.zero, Quaternion.identity, new Vector3(2f, 2f, 1f));
                                cmd.DrawMesh(data.QuadMesh, fullscreenMatrix, data.GlobalLightMaterial, 0, 0);

                                // Draw directional shadow geometry (BlendOp Min, darkens lit areas).
                                if (data.GlobalShadowMesh != null && data.ShadowMaterial != null)
                                {
                                    cmd.DrawMesh(data.GlobalShadowMesh, Matrix4x4.identity, data.ShadowMaterial, 0, 0);
                                }
                            }

                            // ── Point lights ─────────────────────────────────
                            foreach (PointLightData entry in data.PointLightEntries)
                            {
                                // Draw light circle (additive).
                                if (data.LightMaterial != null)
                                {
                                    Vector4 lp = entry.QuadParams;
                                    Matrix4x4 lightMatrix = Matrix4x4.TRS(
                                        new Vector3(lp.x, lp.y, 0f),
                                        Quaternion.identity,
                                        new Vector3(lp.z * 2f, lp.w * 2f, 1f));

                                    cmd.DrawMesh(data.QuadMesh, lightMatrix, data.LightMaterial, 0, 0);
                                }

                                // Draw radial shadow geometry for this point light (BlendOp Min).
                                if (entry.ShadowMesh != null && data.ShadowMaterial != null)
                                {
                                    cmd.DrawMesh(entry.ShadowMesh, Matrix4x4.identity, data.ShadowMaterial, 0, 0);
                                }
                            }
                        }

                        // Set _LightMapParams as a global float4 (screen dimensions).
                        cmd.SetGlobalVector(LIGHT_MAP_PARAMS_ID,
                            new Vector4(data.ScreenWidth, data.ScreenHeight, 0f, 0f));
                    });
                }
            }

            // ── Directional shadow geometry (global light) ───────────────────────

            // Builds a combined directional shadow mesh for all shadow casters.
            // Shadows are parallel, cast in the direction opposite to globalLightDir,
            // with length = MaxShadowLength × intensity.
            private Mesh BuildDirectionalShadowMesh(
                Vector2 globalLightDir, float intensity,
                List<ShadowCasterRenderData> shadowCasters,
                Camera cam, int screenWidth, int screenHeight)
            {
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                Vector2 shadowDir = -globalLightDir.normalized;

                for (int c = 0; c < shadowCasters.Count; c++)
                {
                    ShadowCasterRenderData caster = shadowCasters[c];
                    Vector2[] worldPoints = caster.WorldPoints;
                    if (worldPoints == null || worldPoints.Length < 3)
                    {
                        continue;
                    }

                    float shadowLength = caster.MaxShadowLength * intensity;
                    if (shadowLength <= 0f)
                    {
                        continue;
                    }

                    // Add caster polygon as solid occluder.
                    AddPolygonToMesh(worldPoints, cam, screenWidth, screenHeight, vertices, triangles);

                    // Add directional shadow fins.
                    AddDirectionalShadowFins(worldPoints, shadowDir, shadowLength, cam, screenWidth, screenHeight, vertices, triangles);
                }

                if (vertices.Count == 0)
                {
                    return null;
                }

                Mesh mesh = new Mesh { name = "DirectionalShadowMesh" };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                return mesh;
            }

            // Finds back-facing edges relative to the light direction and extrudes
            // shadow fin quads in a fixed parallel direction.
            private static void AddDirectionalShadowFins(
                Vector2[] worldPoints, Vector2 shadowDir, float shadowLength,
                Camera cam, int screenWidth, int screenHeight,
                List<Vector3> vertices, List<int> triangles)
            {
                int pointCount = worldPoints.Length;

                for (int i = 0; i < pointCount; i++)
                {
                    Vector2 a = worldPoints[i];
                    Vector2 b = worldPoints[(i + 1) % pointCount];

                    // Edge direction and outward normal (assuming CCW winding).
                    Vector2 edge = b - a;
                    Vector2 edgeNormal = new Vector2(-edge.y, edge.x);

                    // Back-facing: edge normal agrees with shadow direction.
                    float dot = Vector2.Dot(edgeNormal, shadowDir);
                    if (dot <= 0f)
                    {
                        continue;
                    }

                    // Parallel extrusion: both vertices extruded in the same fixed direction.
                    Vector2 extrudedA = a + shadowDir * shadowLength;
                    Vector2 extrudedB = b + shadowDir * shadowLength;

                    // Build quad (two triangles).
                    int baseIndex = vertices.Count;
                    vertices.Add(WorldXZToNDC(a, cam, screenWidth, screenHeight));
                    vertices.Add(WorldXZToNDC(b, cam, screenWidth, screenHeight));
                    vertices.Add(WorldXZToNDC(extrudedB, cam, screenWidth, screenHeight));
                    vertices.Add(WorldXZToNDC(extrudedA, cam, screenWidth, screenHeight));

                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);

                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);
                }
            }

            // ── Radial shadow geometry (point lights) ────────────────────────────

            // Builds a combined radial shadow mesh for all shadow casters relevant to a given point light.
            // Returns null if no shadow geometry is needed.
            private Mesh BuildRadialShadowMesh(
                Vector2 lightPosXZ, float lightRadius,
                List<ShadowCasterRenderData> shadowCasters,
                Camera cam, int screenWidth, int screenHeight)
            {
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();

                for (int c = 0; c < shadowCasters.Count; c++)
                {
                    ShadowCasterRenderData caster = shadowCasters[c];
                    Vector2[] worldPoints = caster.WorldPoints;
                    if (worldPoints == null || worldPoints.Length < 3)
                    {
                        continue;
                    }

                    // Distance check: use centroid of world points.
                    Vector2 centroid = ComputeCentroid(worldPoints);
                    float casterExtent = EstimateCasterExtent(worldPoints, centroid);
                    float distance = Vector2.Distance(lightPosXZ, centroid);
                    if (distance > lightRadius + casterExtent + POINT_SHADOW_EXTRUDE_DISTANCE)
                    {
                        continue;
                    }

                    // Add the caster polygon itself as a solid occluder.
                    AddPolygonToMesh(worldPoints, cam, screenWidth, screenHeight, vertices, triangles);

                    // Find back-facing edges and extrude radial shadow fins.
                    AddRadialShadowFins(worldPoints, lightPosXZ, cam, screenWidth, screenHeight, vertices, triangles);
                }

                if (vertices.Count == 0)
                {
                    return null;
                }

                Mesh mesh = new Mesh { name = "RadialShadowMesh" };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                return mesh;
            }

            // Finds back-facing edges of the polygon relative to a point light and
            // extrudes radial shadow fin quads (vertices pushed away from the light).
            private static void AddRadialShadowFins(
                Vector2[] worldPoints, Vector2 lightPosXZ,
                Camera cam, int screenWidth, int screenHeight,
                List<Vector3> vertices, List<int> triangles)
            {
                int pointCount = worldPoints.Length;

                for (int i = 0; i < pointCount; i++)
                {
                    Vector2 a = worldPoints[i];
                    Vector2 b = worldPoints[(i + 1) % pointCount];

                    // Edge direction and outward normal (assuming CCW winding).
                    Vector2 edge = b - a;
                    Vector2 edgeNormal = new Vector2(-edge.y, edge.x);

                    // Direction from light to edge midpoint.
                    Vector2 edgeMid = (a + b) * 0.5f;
                    Vector2 lightToMid = edgeMid - lightPosXZ;

                    // Back-facing: normal points away from the light.
                    float dot = Vector2.Dot(edgeNormal, lightToMid);
                    if (dot <= 0f)
                    {
                        continue;
                    }

                    // Radial extrusion: each vertex pushed away from the light position.
                    Vector2 dirA = (a - lightPosXZ).normalized;
                    Vector2 dirB = (b - lightPosXZ).normalized;
                    Vector2 extrudedA = a + dirA * POINT_SHADOW_EXTRUDE_DISTANCE;
                    Vector2 extrudedB = b + dirB * POINT_SHADOW_EXTRUDE_DISTANCE;

                    // Build quad (two triangles).
                    int baseIndex = vertices.Count;
                    vertices.Add(WorldXZToNDC(a, cam, screenWidth, screenHeight));
                    vertices.Add(WorldXZToNDC(b, cam, screenWidth, screenHeight));
                    vertices.Add(WorldXZToNDC(extrudedB, cam, screenWidth, screenHeight));
                    vertices.Add(WorldXZToNDC(extrudedA, cam, screenWidth, screenHeight));

                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);

                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);
                }
            }

            // ── Shared helpers ───────────────────────────────────────────────────

            // Estimates the maximum distance from the caster center to any of its polygon vertices.
            private static float EstimateCasterExtent(Vector2[] worldPoints, Vector2 center)
            {
                float maxDistSq = 0f;
                foreach (Vector2 point in worldPoints)
                {
                    float distSq = (point - center).sqrMagnitude;
                    if (distSq > maxDistSq)
                    {
                        maxDistSq = distSq;
                    }
                }
                return Mathf.Sqrt(maxDistSq);
            }

            // Computes the centroid of a polygon.
            private static Vector2 ComputeCentroid(Vector2[] points)
            {
                Vector2 sum = Vector2.zero;
                foreach (Vector2 point in points)
                {
                    sum += point;
                }
                return sum / points.Length;
            }

            // Converts a world XZ position to NDC xy for the shadow mesh vertex.
            private static Vector3 WorldXZToNDC(Vector2 xz, Camera cam, int screenWidth, int screenHeight)
            {
                Vector3 worldPos = new Vector3(xz.x, 0f, xz.y);
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                float ndcX = (screenPos.x / screenWidth) * 2f - 1f;
                float ndcY = (screenPos.y / screenHeight) * 2f - 1f;
                return new Vector3(ndcX, ndcY, 0f);
            }

            // Adds a polygon (triangle fan) to the mesh as a solid occluder.
            private static void AddPolygonToMesh(
                Vector2[] worldPoints, Camera cam, int screenWidth, int screenHeight,
                List<Vector3> vertices, List<int> triangles)
            {
                int baseIndex = vertices.Count;

                foreach (Vector2 point in worldPoints)
                {
                    vertices.Add(WorldXZToNDC(point, cam, screenWidth, screenHeight));
                }

                // Triangle fan from the first vertex.
                for (int i = 1; i < worldPoints.Length - 1; i++)
                {
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + i);
                    triangles.Add(baseIndex + i + 1);
                }
            }

            // Creates a simple unit quad mesh centred at origin with UV [0,1].
            private static Mesh CreateQuadMesh()
            {
                Mesh mesh = new Mesh { name = "LightMap Quad" };

                mesh.vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3( 0.5f, -0.5f, 0f),
                    new Vector3( 0.5f,  0.5f, 0f),
                    new Vector3(-0.5f,  0.5f, 0f)
                };

                mesh.uv = new Vector2[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f)
                };

                mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

                mesh.UploadMeshData(true);
                return mesh;
            }
        }
    }
}
