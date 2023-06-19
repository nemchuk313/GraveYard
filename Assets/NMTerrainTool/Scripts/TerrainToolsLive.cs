using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainToolsLive : MonoBehaviour
{
    private Camera _camera;
    [SerializeField] private float m_BrushOpacity = 0.1f;
    [SerializeField] private float m_BrushSize = 50;
    [SerializeField] private float m_BrushRotation = 0;
    [SerializeField] private Texture2D brushTexture;
    [SerializeField] private Transform brush;
    [SerializeField] private float scrollScale = 0.1f;
    private Vector2 _hitUV;
    private Vector3 _hitPosition;
    private Terrain _hitTerrain;

    void Start()
    {
        _camera = Camera.main;
    }
    void Update()
    {
        GetTerrainUV();

        if (!_hitTerrain)
            return;

        m_BrushSize += Input.mouseScrollDelta.y * scrollScale;

        brush.position = _hitPosition;
        brush.localScale = Vector3.one * m_BrushSize;
        if (Input.GetMouseButtonDown(0))
        {
            OnPaint(_hitTerrain, _hitUV);
        }
    }
    private void GetTerrainUV()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

        if (hits.Length <= 0) return;


        foreach (var hit in hits)
        {
            if (hit.collider is not TerrainCollider) continue;

            _hitPosition = hit.point;
            _hitUV = hit.textureCoord;
            _hitTerrain = hit.collider.GetComponent<Terrain>();
            break;
        }
    }
    private void RenderIntoPaintContext(UnityEngine.TerrainTools.PaintContext paintContext, Texture brushTexture, UnityEngine.TerrainTools.BrushTransform brushXform)
    {
        // Get the built-in painting Material reference
        Material mat = UnityEngine.TerrainTools.TerrainPaintUtility.GetBuiltinPaintMaterial();

        // Bind the current brush texture
        mat.SetTexture("_BrushTex", brushTexture);

        // Bind the tool-specific shader properties
        mat.SetVector("_BrushParams", new Vector4(m_BrushOpacity, 0.0f, 0.0f, 0.0f));

        //mat.SetVector("_BrushParams", new Vector4(0, 0.0f, m_BrushHeight, opacity));
        //mat.SetVector("_BrushParams", new Vector4(0, 0.0f, brushHeight, opacity));

        // Setup the material for reading from/writing into the PaintContext texture data. This is a necessary step to setup the correct shader properties for appropriately transforming UVs and sampling textures within the shader
        UnityEngine.TerrainTools.TerrainPaintUtility.SetupTerrainToolMaterialProperties(paintContext, brushXform, mat);

        // Render into the PaintContext's destinationRenderTexture using the built-in painting Material - the id for the Raise/Lower pass is 0.

        Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

        //Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 1);
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