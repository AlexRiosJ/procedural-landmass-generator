using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

    public enum DrawMode { NoiseMap, Mesh, FalloffMap }
    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range (0, MeshSettings.numSupportedLODs - 1)]
    public int editorPreviewLOD;
    public bool autoUpdate;

    float[, ] falloffMap;

    Queue<MapThreadInfo<HeightMap>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<HeightMap>> ();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>> ();

    void Start () {
        textureData.ApplyToMaterial (terrainMaterial);
        textureData.UpdateMeshHeights (terrainMaterial, heightMapSettings.minHieght, heightMapSettings.maxHieght);
    }

    void OnValuesUpdated () {
        if (!Application.isPlaying) {
            DrawMapInEditor ();
        }
    }

    void OnTextureValuesUpdated () {
        textureData.ApplyToMaterial (terrainMaterial);
    }

    public void DrawMapInEditor () {
        textureData.UpdateMeshHeights (terrainMaterial, heightMapSettings.minHieght, heightMapSettings.maxHieght);

        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap (meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay> ();
        if (drawMode == DrawMode.NoiseMap) {
            display.DrawTexture (TextureGenerator.TextureFromHeightMap (heightMap.values));
        } else if (drawMode == DrawMode.Mesh) {
            display.DrawMesh (MeshGenerator.GenerateTerrrainMesh (heightMap.values, meshSettings, editorPreviewLOD));
        } else if (drawMode == DrawMode.FalloffMap) {
            display.DrawTexture (TextureGenerator.TextureFromHeightMap (FalloffGenerator.GenerateFalloffMap (meshSettings.numVertsPerLine)));
        }
    }

    public void RequestMapData (Vector2 center, Action<HeightMap> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread (center, callback);
        };

        new Thread (threadStart).Start ();
    }

    void MapDataThread (Vector2 center, Action<HeightMap> callback) {
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap (meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, center);

        lock (mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<HeightMap> (callback, heightMap));
        }
    }

    public void RequestMeshData (HeightMap heightMap, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread (heightMap, lod, callback);
        };

        new Thread (threadStart).Start ();
    }

    void MeshDataThread (HeightMap heightMap, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrrainMesh (heightMap.values, meshSettings, lod);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData> (callback, meshData));
        }
    }

    void Update () {
        if (mapDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<HeightMap> threadInfo = mapDataThreadInfoQueue.Dequeue ();
                threadInfo.callback (threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue ();
                threadInfo.callback (threadInfo.parameter);
            }
        }
    }

    void OnValidate () {
        if (heightMapSettings != null) {
            heightMapSettings.OnValueUpdated -= OnValuesUpdated;
            heightMapSettings.OnValueUpdated += OnValuesUpdated;
        }
        if (heightMapSettings != null) {
            heightMapSettings.OnValueUpdated -= OnValuesUpdated;
            heightMapSettings.OnValueUpdated += OnValuesUpdated;
        }
        if (textureData != null) {
            textureData.OnValueUpdated -= OnTextureValuesUpdated;
            textureData.OnValueUpdated += OnTextureValuesUpdated;
        }
    }

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo (Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

}