using System;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{

    [SerializeField]
    private Texture2D textureToChangeMipMap;

    public int mapSizeInChunks = 24;
    public int chunkSize = 16, chunkHeight = 128;
    public int waterLevel = 50, snowLevel = 100;
    public float noiseScaleMin = 0.001f, noiseScaleMax = 0.02f;
    private float noiseScale;
    
    private GameObject chunkPrefab;
    private Transform chunksParent;
    private Dictionary<Vector3Int, ChunkData> chunkDataDictionary = new Dictionary<Vector3Int, ChunkData>();
    private Dictionary<Vector3Int, ChunkRenderer> chunkDictionary = new Dictionary<Vector3Int, ChunkRenderer>();

    public void Start()
    {
        chunkPrefab = Resources.Load<GameObject>("Prefabs/Chunk");
        
        GameObject parentObject = new GameObject("Chunks");
        chunksParent = parentObject.transform;

        GenerateWorld();
    }
    
    public void GenerateWorld()
    {
        chunkDataDictionary.Clear();
        foreach (ChunkRenderer chunk in chunkDictionary.Values)
        {
            Destroy(chunk.gameObject);
        }
        chunkDictionary.Clear();

        //noiseScale = UnityEngine.Random.Range(noiseScaleMin, noiseScaleMax);
        noiseScale = noiseScaleMin;

        for (int x = 0; x < mapSizeInChunks; x++)
        {
            for (int z = 0; z < mapSizeInChunks; z++)
            {
                ChunkData data = new ChunkData(chunkSize, chunkHeight, this, new Vector3Int(x * chunkSize, 0, z * chunkSize));
                GenerateBlocks(data);
                chunkDataDictionary.Add(data.worldPosition, data);
            }
        }

        foreach (ChunkData data in chunkDataDictionary.Values)
        {
            MeshData meshData = Chunk.GetChunkMeshData(data);
            GameObject chunkObject = Instantiate(chunkPrefab, data.worldPosition, Quaternion.identity, chunksParent);
            ChunkRenderer chunkRenderer = chunkObject.GetComponent<ChunkRenderer>();
            chunkDictionary.Add(data.worldPosition, chunkRenderer);
            chunkRenderer.InitChunk(data);
            chunkRenderer.UpdateChunk(meshData);

        }
    }

    private void GenerateBlocks(ChunkData data)
    {
        float noiseValue;
        int groundLevel;

        for (int x = 0; x < data.chunkSize; x++)
        {
            for (int z = 0; z < data.chunkSize; z++)
            {
                noiseValue = Mathf.PerlinNoise(
                    (data.worldPosition.x + x) * noiseScale,
                    (data.worldPosition.z + z) * noiseScale
                );

                groundLevel = Mathf.RoundToInt(noiseValue * chunkHeight);

                for (int y = chunkHeight - 1; y >= 0; y--)
                {
                    BlockType blockType;

                    // Above terrain
                    if (y > groundLevel)
                    {
                        blockType = (y <= waterLevel) ? BlockType.Water : BlockType.Air;
                    }
                    // Surface block
                    else if (y == groundLevel)
                    {
                        if (groundLevel >= snowLevel)
                        {
                            blockType = BlockType.Snow;
                        }
                        else if (groundLevel <= waterLevel)
                        {
                            blockType = BlockType.Sand;
                        }
                        else
                        {
                            blockType = BlockType.Grass;
                        }
                    }
                    // Underground blocks
                    else
                    {
                        int depth = groundLevel - y;
                        if (groundLevel >= snowLevel)
                        {
                            // Under snow: snow till snowLevel, then rock
                            blockType = (y >= snowLevel) ? BlockType.Snow : BlockType.Rock;
                        }
                        else if (groundLevel <= waterLevel)
                        {
                            // Under water: 5 blocks of sand, then rock
                            blockType = (depth <= 5) ? BlockType.Sand : BlockType.Rock;
                        }
                        else
                        {
                            // Under grass: 5 blocks of dirt, then rock
                            blockType = (depth <= 5) ? BlockType.Dirt : BlockType.Rock;
                        }
                    }

                    Chunk.SetBlock(data, new Vector3Int(x, y, z), blockType);
                }
            }
        }
    }

    internal BlockType GetBlockFromChunkCoordinates(ChunkData chunkData, int x, int y, int z)
    {
        Vector3Int pos = Chunk.ChunkPositionFromBlockCoords(this, x, y, z);
        ChunkData containerChunk = null;

        chunkDataDictionary.TryGetValue(pos, out containerChunk);

        if (containerChunk == null)
            return BlockType.Nothing;
        Vector3Int blockInCHunkCoordinates = Chunk.GetBlockInChunkCoordinates(containerChunk, new Vector3Int(x, y, z));
        return Chunk.GetBlockFromCoordinatesInChunk(containerChunk, blockInCHunkCoordinates);
    }
}