using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;

    public Transform viewer;
    public Material mapMaterial;

    Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    float meshWorldSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk> ();
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk> ();

    void Start () {
        textureSettings.ApplyToMaterial (mapMaterial);
        textureSettings.UpdateMeshHeights (mapMaterial, heightMapSettings.minHieght, heightMapSettings.maxHieght);

        float maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        meshWorldSize = meshSettings.meshWorldSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt (maxViewDistance / meshWorldSize);
        UpdateVisibleChunks ();
    }

    void Update () {
        viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);

        if (viewerPosition != viewerPositionOld) {
            foreach (TerrainChunk chunk in visibleTerrainChunks) {
                chunk.UpdateCollisionMesh ();
            }
        }

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks ();
        }
    }

    void UpdateVisibleChunks () {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2> ();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--) {
            alreadyUpdatedChunkCoords.Add (visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk ();
        }

        int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / meshWorldSize);

        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (!alreadyUpdatedChunkCoords.Contains (viewedChunkCoord)) {
                    if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk ();
                    } else {
                        TerrainChunk newChunk = new TerrainChunk (viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                        terrainChunkDictionary.Add (viewedChunkCoord, newChunk);
                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load ();
                    }
                }
            }
        }
    }

    void OnTerrainChunkVisibilityChanged (TerrainChunk chunk, bool isVisible) {
        if (isVisible) {
            visibleTerrainChunks.Add (chunk);
        } else {
            visibleTerrainChunks.Remove (chunk);
        }
    }

}

[System.Serializable]
public struct LODInfo {
    [Range (0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public float visibleDstThreshold;

    public float sqrVisibleDstThreshold {
        get {
            return visibleDstThreshold * visibleDstThreshold;
        }
    }
}