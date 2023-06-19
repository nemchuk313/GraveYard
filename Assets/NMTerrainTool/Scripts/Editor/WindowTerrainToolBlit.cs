using System;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.Rendering;

public class WindowTerrainToolBlit : EditorWindow
{
    private float m_BrushHeight;
    private float brushHeight;
    private float m_BrushOpacity;
    private float m_BrushSize;
    private float m_BrushRotation;
    private RenderTexture brushTexture;
    private Vector2 _hitUV;
    private Terrain _hitTerrain;
    private MeshFilter _meshFilter;
    private float _terrainHeight;
    private Vector3 _brushCenter;
    private Bounds _brushBounds;
    private bool _paint = false;

    [MenuItem("Tools/Window Terrain Tool Blit")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(WindowTerrainToolBlit), false, "Window Terrain Tool Blit");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        // Add (or re-add) the delegate.
        SceneView.duringSceneGui += OnSceneGUI;

        brushTexture = RenderTexture.GetTemporary((int) 512, (int) 512, 0, RenderTextureFormat.ARGB64);
    }

    void OnDestroy()
    {
        // When the window is destroyed, remove the delegate
        // so that it will no longer do any drawing.
        SceneView.duringSceneGui -= this.OnSceneGUI;

        if (brushTexture != null)
            RenderTexture.ReleaseTemporary(brushTexture);
    }

    void OnGUI()
    {
        // m_BrushHeight = EditorGUILayout.Slider("Height", m_BrushHeight, 0, 1);
        // brushHeight = m_BrushHeight / terrain.terrainData.size.y;
        _meshFilter = (MeshFilter) EditorGUILayout.ObjectField("Mesh filter", _meshFilter, typeof(MeshFilter), true);
        m_BrushOpacity = EditorGUILayout.Slider("Opacity", m_BrushOpacity, 0, 10);
        // m_BrushSize = EditorGUILayout.Slider("Size", m_BrushSize, .001f, 8096);
        brushTexture = (RenderTexture) EditorGUILayout.ObjectField("Brush texture", brushTexture, typeof(RenderTexture), false);
        _paint = EditorGUILayout.Toggle("Paint terrain", _paint);

        if (GUILayout.Button("Carve"))
            OnPaint(_hitTerrain, _hitUV);
    }


    private void GenerateDepthMask()
    {
        GameObject cameraGameObject = new GameObject();

        Camera depthCamera = cameraGameObject.AddComponent<Camera>();
        Vector3 position = _brushCenter;
        position.y = _brushBounds.max.y + 10;

        depthCamera.transform.position = position;
        depthCamera.transform.LookAt(_brushCenter);
        depthCamera.nearClipPlane = 0.1f;
        depthCamera.farClipPlane = _brushBounds.size.y + 10;
        depthCamera.orthographic = true;
        depthCamera.orthographicSize = m_BrushSize * 0.5f;
        depthCamera.rect = new Rect(0, 0, 1, 1);

        Material depthMaterial = new Material(Shader.Find("Hidden/NatureManufacture Shaders/WorldDepth"));

        depthMaterial.SetFloat("_heightMax", _terrainHeight);
        Matrix4x4 orthoMatrix = Matrix4x4.Ortho(-m_BrushSize * 0.5f, m_BrushSize * 0.5f, -m_BrushSize * 0.5f, m_BrushSize * 0.5f, 2, 2000);


        CommandBuffer depthCommandBuffer = new CommandBuffer();
        depthCommandBuffer.name = "ModelWorldDepthBaker";


        depthCommandBuffer.Clear();
        depthCommandBuffer.SetRenderTarget(brushTexture);
        depthCommandBuffer.ClearRenderTarget(true, true, Color.black);
        depthCommandBuffer.SetViewProjectionMatrices(depthCamera.worldToCameraMatrix, orthoMatrix);
        depthCommandBuffer.DrawMesh(_meshFilter.sharedMesh, _meshFilter.transform.localToWorldMatrix, depthMaterial, 0);
        Graphics.ExecuteCommandBuffer(depthCommandBuffer);


        depthCommandBuffer.Release();
        DestroyImmediate(cameraGameObject);
    }

    private void GenerateBounds()
    {
        MeshRenderer renderer = _meshFilter.GetComponent<MeshRenderer>();

        var bounds = renderer.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        float extent = Mathf.Max(extents.x, extents.z);

        float minX = center.x - extent;
        float maxX = center.x + extent;
        m_BrushSize = maxX - minX;

        _brushCenter = center;
        _brushBounds = bounds;
    }


    private void OnSceneGUI(SceneView sceneview)
    {
        if (!_paint)
            return;
        if (_meshFilter == null)
            return;

        GenerateBounds();
        GetTerrainUV();


        if (!_hitTerrain)
            return;


        GenerateDepthMask();


        Event e = Event.current;

        /*
        if (e.button == 0 && e.isMouse && e.type == EventType.MouseDown)
        {
            OnPaint(_hitTerrain, _hitUV);
            // GUIUtility.hotControl = controlID;
            e.Use();
        }
*/

        OnRenderBrushPreview(_hitTerrain, _hitUV);
    }

    private void GetTerrainUV()
    {
        Ray ray = new Ray(_brushCenter + Vector3.up * 1000, Vector3.down);

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

        if (hits.Length <= 0) return;


        foreach (var hit in hits)
        {
            if (hit.collider is not TerrainCollider) continue;


            _hitUV = hit.textureCoord;
            _hitTerrain = hit.collider.GetComponent<Terrain>();
            _terrainHeight = _hitTerrain.terrainData.size.y;
            break;
        }
    }

    private void RenderIntoPaintContext(UnityEngine.TerrainTools.PaintContext paintContext, Texture brushTexture, UnityEngine.TerrainTools.BrushTransform brushXform)
    {
        // Get the built-in painting Material reference
        //Material mat = UnityEngine.TerrainTools.TerrainPaintUtility.GetBuiltinPaintMaterial();
        Material mat = new Material(Shader.Find("Hidden/TerrainEngine/ChangeHeight"));
        // Bind the current brush texture
        mat.SetTexture("_BrushTex", brushTexture);

        // Bind the tool-specific shader properties
        var opacity = Event.current.control ? -m_BrushOpacity : m_BrushOpacity;
        mat.SetVector("_BrushParams", new Vector4(opacity*0.5f, 0.0f, 0.0f, 0.0f));

        //mat.SetVector("_BrushParams", new Vector4(0, 0.0f, m_BrushHeight, opacity));
        //mat.SetVector("_BrushParams", new Vector4(0, 0.0f, brushHeight, opacity));

        // Setup the material for reading from/writing into the PaintContext texture data. This is a necessary step to setup the correct shader properties for appropriately transforming UVs and sampling textures within the shader
        UnityEngine.TerrainTools.TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

        // Render into the PaintContext's destinationRenderTexture using the built-in painting Material - the id for the Raise/Lower pass is 0.

        Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

        //Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 1);
    }

    public void OnRenderBrushPreview(Terrain terrain, Vector2 terrainUV)
    {
        // Dont render preview if this isnt a Repaint
        if (Event.current.type != EventType.Repaint) return;

        if (terrain == null)
            return;

        // Get the current BrushTransform under the mouse position relative to the Terrain
        UnityEngine.TerrainTools.BrushTransform brushXform = UnityEngine.TerrainTools.TerrainPaintUtility.CalculateBrushTransform(terrain, terrainUV, m_BrushSize, m_BrushRotation);

        // Get the PaintContext for the current BrushTransform. This has a sourceRenderTexture from which to read existing Terrain texture data.
        UnityEngine.TerrainTools.PaintContext paintContext = UnityEngine.TerrainTools.TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

        // Get the built-in Material for rendering Brush Previews
        Material previewMaterial = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();

        // Render the brush preview for the sourceRenderTexture. This will show up as a projected brush mesh rendered on top of the Terrain
        TerrainPaintUtilityEditor.DrawBrushPreview(paintContext, TerrainBrushPreviewMode.SourceRenderTexture, brushTexture, brushXform, previewMaterial, 0);

        // Render changes into the PaintContext destinationRenderTexture
        RenderIntoPaintContext(paintContext, brushTexture, brushXform);

        // Restore old render target.
        RenderTexture.active = paintContext.oldRenderTexture;

        // Bind the sourceRenderTexture to the preview Material. This is used to compute deltas in height
        previewMaterial.SetTexture("_HeightmapOrig", paintContext.sourceRenderTexture);

        // Render a procedural mesh displaying the delta/displacement in height from the source Terrain texture data. When modifying Terrain height, this shows how much the next paint operation will alter the Terrain height
        TerrainPaintUtilityEditor.DrawBrushPreview(paintContext, TerrainBrushPreviewMode.DestinationRenderTexture, brushTexture, brushXform, previewMaterial, 2);

        // Cleanup resources
        UnityEngine.TerrainTools.TerrainPaintUtility.ReleaseContextResources(paintContext);
    }

    public bool OnPaint(Terrain terrain, Vector2 terrainUV)
    {
        // Get the current BrushTransform under the mouse position relative to the Terrain
        UnityEngine.TerrainTools.BrushTransform brushXform = UnityEngine.TerrainTools.TerrainPaintUtility.CalculateBrushTransform(terrain, terrainUV, m_BrushSize, m_BrushRotation);

        // Get the PaintContext for the current BrushTransform. This has a sourceRenderTexture from which to read existing Terrain texture data
        // and a destinationRenderTexture into which to write new Terrain texture data
        UnityEngine.TerrainTools.PaintContext paintContext = UnityEngine.TerrainTools.TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());

        // Call the common rendering function used by OnRenderBrushPreview and OnPaint
        RenderIntoPaintContext(paintContext, brushTexture, brushXform);


        // Commit the modified PaintContext with a provided string for tracking Undo operations. This function handles Undo and resource cleanup for you
        UnityEngine.TerrainTools.TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Raise or Lower Height");

        // Return whether or not Trees and Details should be hidden while painting with this Terrain Tool
        return true;
    }
}