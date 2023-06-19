using UnityEngine;
using UnityEditor;

public class WindowRandomHeights : EditorWindow
{
    [MenuItem("Tools/Random Heights")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(WindowRandomHeights), false, "Random Heights");
    }

    private float _strength = 0.1f;
    private float _noiseSize = 1f;

    private void OnGUI()
    {
        _strength = EditorGUILayout.Slider("Strength", _strength, 0, 1);
        _noiseSize = EditorGUILayout.Slider("Noise Size", _noiseSize, 0, 1000);

        if (GUILayout.Button("Random heights"))
        {
            SetRandomHeights(_strength, _noiseSize);
        }

        if (GUILayout.Button("Zero heights"))
        {
            SetZeroHeights();
        }
    }

    void SetRandomHeights(float strength, float noiseSize)
    {
        Terrain terrain = Terrain.activeTerrain;
        TerrainData tData = terrain.terrainData;

        int resolution = tData.heightmapResolution;
        float[,] heights = tData.GetHeights(0, 0, resolution, resolution);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                heights[x, y] = Mathf.PerlinNoise(x / noiseSize, y / noiseSize) * strength;
            }
        }

        tData.SetHeights(0, 0, heights);
    }


    void SetZeroHeights()
    {
        Terrain terrain = Terrain.activeTerrain;
        TerrainData tData = terrain.terrainData;

        int resolution = tData.heightmapResolution;
        float[,] heights = tData.GetHeights(0, 0, resolution, resolution);

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                heights[x, y] = 0;
            }
        }

        tData.SetHeights(0, 0, heights);
    }
}