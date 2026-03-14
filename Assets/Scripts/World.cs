using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class World : MonoBehaviour
{
    public int mapSizeInChunks = 24;
    public int chunkSize = 16, chunkHeight = 128, chunkDrawingRange = 8;
    public int waterLevel = 50, snowLevel = 100;
    public float noiseScaleMin = 0.001f, noiseScaleMax = 0.02f;
    private float noiseScale, noiseOffsetX, noiseOffsetZ;
    
    private GameObject chunkPrefab;
    private Transform chunksParent;

    public UnityEvent OnWorldCreated, OnNewChunksGenerated;

    public WorldData worldData { get; private set; }

    private void Awake()
    {
        worldData = new WorldData
        {
            chunkHeight = this.chunkHeight,
            chunkSize = this.chunkSize,
            chunkDataDictionary = new Dictionary<Vector3Int, ChunkData>(),
            chunkPositionsDictionary = new Dictionary<Vector3Int, ChunkRenderer>()
        };

        noiseScale = UnityEngine.Random.Range(noiseScaleMin, noiseScaleMax);
        noiseOffsetX = UnityEngine.Random.Range(10000f, 100000f);
        noiseOffsetZ = UnityEngine.Random.Range(10000f, 100000f);
    }

    public void Start()
    {
        chunkPrefab = Resources.Load<GameObject>("Prefabs/Chunk");
        
        GameObject parentObject = new GameObject("Chunks");
        chunksParent = parentObject.transform;

        GenerateWorld();
    }

    public void GenerateWorld()
    {
        GenerateWorld(Vector3Int.zero);
    }

    private void GenerateWorld(Vector3Int position)
    {
        WorldGenerationData worldGenerationData = GetPositionsThatPlayerSees(position);

        foreach (var pos in worldGenerationData.chunkPositionsToRemove)
        {
            RemoveChunkPosition(pos);
        }

        foreach (var pos in worldGenerationData.chunkDataToRemove)
        {
            RemoveChunkData(pos);
        }

        foreach (var pos in worldGenerationData.chunkDataToCreate)
        {
            ChunkData data = new ChunkData(chunkSize, chunkHeight, this, pos);
            GenerateBlocks(data);
            worldData.chunkDataDictionary.Add(pos, data);
        }

        foreach (var pos in worldGenerationData.chunkPositionsToCreate)
        {
            ChunkData data = worldData.chunkDataDictionary[pos];
            MeshData meshData = Chunk.GetChunkMeshData(data);
            GameObject chunkObject = Instantiate(chunkPrefab, data.worldPosition, Quaternion.identity, chunksParent);
            ChunkRenderer chunkRenderer = chunkObject.GetComponent<ChunkRenderer>();
            worldData.chunkPositionsDictionary.Add(data.worldPosition, chunkRenderer);
            chunkRenderer.InitChunk(data);
            chunkRenderer.UpdateChunk(meshData);
        }

        OnWorldCreated?.Invoke();
    }

    private WorldGenerationData GetPositionsThatPlayerSees(Vector3Int playerPosition)
    {
        List<Vector3Int> allChunkPositionsNeeded = GetChunkPositionsAroundPlayer(playerPosition);
        List<Vector3Int> allChunkDataNeeded = GetChunkDataAroundPlayer(playerPosition);

        List<Vector3Int> chunkPositionsToCreate = SelectChunkPositionsToCreate(worldData, allChunkPositionsNeeded, playerPosition);
        List<Vector3Int> chunkDataToCreate = SelectChunkDataToCreate(worldData, allChunkDataNeeded, playerPosition);

        List<Vector3Int> chunkPositionsToRemove = GetRedundantChunksPositions(worldData, allChunkPositionsNeeded);
        List<Vector3Int> chunkDataToRemove = GetRedundantChunksData(worldData, allChunkDataNeeded);

        WorldGenerationData data = new WorldGenerationData
        {
            chunkPositionsToCreate = chunkPositionsToCreate,
            chunkDataToCreate = chunkDataToCreate,
            chunkPositionsToRemove = chunkPositionsToRemove,
            chunkDataToRemove = chunkDataToRemove,
        };
        return data;
    }

    private void RemoveChunk(ChunkRenderer chunk)
    {
        chunk.gameObject.SetActive(false);
    }

    private void RemoveChunkData(Vector3Int pos)
    {
        worldData.chunkDataDictionary.Remove(pos);
    }

    private void RemoveChunkPosition(Vector3Int pos)
    {
        ChunkRenderer chunk = null;
        if (worldData.chunkPositionsDictionary.TryGetValue(pos, out chunk))
        {
            RemoveChunk(chunk);
            worldData.chunkPositionsDictionary.Remove(pos);
        }
    }

    private List<Vector3Int> GetRedundantChunksData(WorldData worldData, List<Vector3Int> allChunkDataPositionsNeeded)
    {
        return worldData.chunkDataDictionary.Keys
            .Where(pos => allChunkDataPositionsNeeded.Contains(pos) == false && worldData.chunkDataDictionary[pos].modifiedByThePlayer == false)
            .ToList();
    }

    private List<Vector3Int> GetRedundantChunksPositions(WorldData worldData, List<Vector3Int> allChunkPositionsNeeded)
    {
        List<Vector3Int> positionToRemove = new List<Vector3Int>();
        foreach (var pos in worldData.chunkPositionsDictionary.Keys
            .Where(pos => allChunkPositionsNeeded.Contains(pos) == false))
        {
            if (worldData.chunkPositionsDictionary.ContainsKey(pos))
            {
                positionToRemove.Add(pos);

            }
        }

        return positionToRemove;
    }

    private List<Vector3Int> GetChunkPositionsAroundPlayer(Vector3Int playerPosition)
    {
        int startX = playerPosition.x - (chunkDrawingRange) * chunkSize;
        int endX = playerPosition.x + (chunkDrawingRange) * chunkSize;
        int startZ = playerPosition.z - (chunkDrawingRange) * chunkSize;
        int endZ = playerPosition.z + (chunkDrawingRange) * chunkSize;

        List<Vector3Int> chunkPositionsToCreate = new List<Vector3Int>();
        for (int x = startX; x <= endX; x += chunkSize)
        {
            for (int z = startZ; z <= endZ; z += chunkSize)
            {
                Vector3Int chunkPos = Chunk.ChunkPositionFromBlockCoords(this, new Vector3Int(x, 0, z));
                chunkPositionsToCreate.Add(chunkPos);
            }
        }

        return chunkPositionsToCreate;
    }

    private List<Vector3Int> GetChunkDataAroundPlayer(Vector3Int playerPosition)
    {
        int startX = playerPosition.x - (chunkDrawingRange + 1) * chunkSize;
        int endX = playerPosition.x + (chunkDrawingRange + 1) * chunkSize;
        int startZ = playerPosition.z - (chunkDrawingRange + 1) * chunkSize;
        int endZ = playerPosition.z + (chunkDrawingRange + 1) * chunkSize;

        List<Vector3Int> chunkDataPositionsToCreate = new List<Vector3Int>();
        for (int x = startX; x <= endX; x += chunkSize)
        {
            for (int z = startZ; z <= endZ; z += chunkSize)
            {
                Vector3Int chunkPos = Chunk.ChunkPositionFromBlockCoords(this, new Vector3Int(x, 0, z));
                chunkDataPositionsToCreate.Add(chunkPos);
            }
        }

        return chunkDataPositionsToCreate;
    }

    private List<Vector3Int> SelectChunkPositionsToCreate(World.WorldData worldData, List<Vector3Int> allChunkPositionsNeeded, Vector3Int playerPosition)
    {
        return allChunkPositionsNeeded
            .Where(pos => worldData.chunkPositionsDictionary.ContainsKey(pos) == false)
            .OrderBy(pos => Vector3.Distance(playerPosition, pos))
            .ToList();
    }

    private List<Vector3Int> SelectChunkDataToCreate(World.WorldData worldData, List<Vector3Int> allChunkDataNeeded, Vector3Int playerPosition)
    {
        return allChunkDataNeeded
            .Where(pos => worldData.chunkDataDictionary.ContainsKey(pos) == false)
            .OrderBy(pos => Vector3.Distance(playerPosition, pos))
            .ToList();
    }
    private void GenerateBlocks(ChunkData data)
    {
        float noiseValue;
        int groundLevel;

        for (int x = 0; x < data.chunkSize; x++)
        {
            for (int z = 0; z < data.chunkSize; z++)
            {
                noiseValue = Mathf.Clamp01(Mathf.PerlinNoise(
                    (data.worldPosition.x + x + noiseOffsetX) * noiseScale,
                    (data.worldPosition.z + z + noiseOffsetZ) * noiseScale
                ));

                groundLevel = Mathf.RoundToInt(noiseValue * (chunkHeight - 3));

                for (int y = 0; y < chunkHeight - 1; y++)
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

        worldData.chunkDataDictionary.TryGetValue(pos, out containerChunk);

        if (containerChunk == null)
            return BlockType.Nothing;
        Vector3Int blockInCHunkCoordinates = Chunk.GetBlockInChunkCoordinates(containerChunk, new Vector3Int(x, y, z));
        return Chunk.GetBlockFromCoordinatesInChunk(containerChunk, blockInCHunkCoordinates);
    }

    internal void LoadAdditionalChunksRequest(GameObject player)
    {
        GenerateWorld(Vector3Int.RoundToInt(player.transform.position));
        OnNewChunksGenerated?.Invoke();
    }

    public struct WorldGenerationData
    {
        public List<Vector3Int> chunkPositionsToCreate;
        public List<Vector3Int> chunkPositionsToRemove;
        public List<Vector3Int> chunkDataToCreate;
        public List<Vector3Int> chunkDataToRemove;
    }

    public struct WorldData
    {
        public Dictionary<Vector3Int, ChunkData> chunkDataDictionary;
        public Dictionary<Vector3Int, ChunkRenderer> chunkPositionsDictionary;
        public int chunkSize;
        public int chunkHeight;
    }
}