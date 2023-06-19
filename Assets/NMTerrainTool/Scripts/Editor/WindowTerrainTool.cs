using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

public class WindowTerrainTool : EditorWindow
{
    private float m_BrushHeight;
    private float brushHeight;
    private float m_BrushOpacity;
    private float m_BrushSize;
    private float m_BrushRotation;
    private Texture2D brushTexture;
    private Vector2 _hitUV;
    private Terrain _hitTerrain;
    private bool _paint = false;

    [MenuItem("Tools/Window Terrain Tool")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(WindowTerrainTool), false, "Window Terrain Tool");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        // Add (or re-add) the delegate.
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
    }

    void OnDestroy()
    {
        // When the window is destroyed, remove the delegate
        // so that it will no longer do any drawing.
        SceneView.duringSceneGui -= this.OnSceneGUI;
    }

    void OnGUI()
    {
        brushTexture = (Texture2D)EditorGUILayout.ObjectField("Brush texture", brushTexture, typeof(Texture2D), false);
        m_BrushOpacity = EditorGUILayout.Slider("Opacity", m_BrushOpacity, 0, 2);
        m_BrushSize = EditorGUILayout.Slider("Size", m_BrushSize, .001f, 8096);
        m_BrushRotation = EditorGUILayout.Slider("Rotation", m_BrushRotation, 0, 360);
        _paint = EditorGUILayout.Toggle("Paint terrain", _paint);

        if (GUILayout.Button("Position brush on whole Terrain"))
        {
            PositionBrush();
        }

        if (GUILayout.Button("Paint brush on whole Terrain"))
        {
            PositionBrush();
            OnPaint(_hitTerrain, _hitUV);
        }
    }

    private bool PositionBrush()
    {
        GameObject go = Terrain.activeTerrain.gameObject;
        if (go == null)
            return true;
        Terrain terrain = go.GetComponent<Terrain>();
        if (terrain != null)
        {
            _hitUV = new Vector2(0.5f, 0.5f);
            _hitTerrain = terrain;
            m_BrushSize = terrain.terrainData.size.x;
        }

        return false;
    }

    private void OnSceneGUI(SceneView sceneview)
    {
        if (!_paint)
            return;
        GetTerrainUV();

        if (!_hitTerrain)
            return;

        Event e = Event.current;

        if (e.button == 0 && e.isMouse && e.type == EventType.MouseDown)
        {
            OnPaint(_hitTerrain, _hitUV);
            // GUIUtility.hotControl = controlID;
            e.Use();
        }


        OnRenderBrushPreview(_hitTerrain, _hitUV);
    }

    private void GetTerrainUV()
    {
        _hitTerrain = null;
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

        if (hits.Length <= 0) return;


        foreach (var hit in hits)
        {
            if (hit.collider is not TerrainCollider) continue;


            _hitUV = hit.textureCoord;
            _hitTerrain = hit.collider.GetComponent<Terrain>();
            break;
        }
    }

    private void RenderIntoPaintContext(UnityEngine.TerrainTools.PaintContext paintContext, Texture brushTexture,
        UnityEngine.TerrainTools.BrushTransform brushXform)
    {
        // Get the built-in painting Material reference
        Material mat = UnityEngine.TerrainTools.TerrainPaintUtility.GetBuiltinPaintMaterial();
        //Material mat = new Material(Shader.Find("Hidden/TerrainEngine/ChangeHeight"));
        // Bind the current brush texture
        mat.SetTexture("_BrushTex", brushTexture);

        // Bind the tool-specific shader properties
        var opacity = Event.current.control ? -m_BrushOpacity : m_BrushOpacity;
        mat.SetVector("_BrushParams", new Vector4(opacity, 0.0f, 0.0f, 0.0f));

        // Setup the material for reading from/writing into the PaintContext texture data. This is a necessary step to setup the correct shader properties for appropriately transforming UVs and sampling textures within the shader
        UnityEngine.TerrainTools.TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

        // Render into the PaintContext's destinationRenderTexture using the built-in painting Material - the id for the Raise/Lower pass is 0.

        Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);
    }

    public void OnRenderBrushPreview(Terrain terrain, Vector2 terrainUV)
    {
        // Dont render preview if this isnt a Repaint
        if (Event.current.type != EventType.Repaint) return;

        if (terrain == null)
            return;

        // Get the current BrushTransform under the mouse position relative to the Terrain
        UnityEngine.TerrainTools.BrushTransform brushXform =
            UnityEngine.TerrainTools.TerrainPaintUtility.CalculateBrushTransform(terrain, terrainUV, m_BrushSize,
                m_BrushRotation);

        // Get the PaintContext for the current BrushTransform. This has a sourceRenderTexture from which to read existing Terrain texture data.
        UnityEngine.TerrainTools.PaintContext paintContext =
            UnityEngine.TerrainTools.TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds(), 1);

        // Get the built-in Material for rendering Brush Previews
        Material previewMaterial = TerrainPaintUtilityEditor.GetDefaultBrushPreviewMaterial();

        // Render the brush preview for the sourceRenderTexture. This will show up as a projected brush mesh rendered on top of the Terrain
        TerrainPaintUtilityEditor.DrawBrushPreview(paintContext, TerrainBrushPreviewMode.SourceRenderTexture,
            brushTexture, brushXform, previewMaterial, 0);

        // Render changes into the PaintContext destinationRenderTexture
        RenderIntoPaintContext(paintContext, brushTexture, brushXform);

        // Restore old render target.
        RenderTexture.active = paintContext.oldRenderTexture;

        // Bind the sourceRenderTexture to the preview Material. This is used to compute deltas in height
        previewMaterial.SetTexture("_HeightmapOrig", paintContext.sourceRenderTexture);

        // Render a procedural mesh displaying the delta/displacement in height from the source Terrain texture data. When modifying Terrain height, this shows how much the next paint operation will alter the Terrain height
        TerrainPaintUtilityEditor.DrawBrushPreview(paintContext, TerrainBrushPreviewMode.DestinationRenderTexture,
            brushTexture, brushXform, previewMaterial, 2);

        // Cleanup resources
        UnityEngine.TerrainTools.TerrainPaintUtility.ReleaseContextResources(paintContext);
    }

    public bool OnPaint(Terrain terrain, Vector2 terrainUV)
    {
        // Get the current BrushTransform under the mouse position relative to the Terrain
        UnityEngine.TerrainTools.BrushTransform brushXform =
            UnityEngine.TerrainTools.TerrainPaintUtility.CalculateBrushTransform(terrain, terrainUV, m_BrushSize,
                m_BrushRotation);

        // Get the PaintContext for the current BrushTransform. This has a sourceRenderTexture from which to read existing Terrain texture data
        // and a destinationRenderTexture into which to write new Terrain texture data
        UnityEngine.TerrainTools.PaintContext paintContext =
            UnityEngine.TerrainTools.TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());

        // Call the common rendering function used by OnRenderBrushPreview and OnPaint
        RenderIntoPaintContext(paintContext, brushTexture, brushXform);


        // Commit the modified PaintContext with a provided string for tracking Undo operations. This function handles Undo and resource cleanup for you
        UnityEngine.TerrainTools.TerrainPaintUtility.EndPaintHeightmap(paintContext,
            "Terrain Paint - Raise or Lower Height");

        // Return whether or not Trees and Details should be hidden while painting with this Terrain Tool
        return true;
    }
}