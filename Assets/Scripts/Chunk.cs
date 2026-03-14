using System;
using UnityEngine;

public static class Chunk
{

    public static void LoopThroughTheBlocks(ChunkData chunkData, Action<int, int, int> actionToPerform)
    {
        for (int index = 0; index < chunkData.blocks.Length; index++)
        {
            var position = GetPositionFromIndex(chunkData, index);
            actionToPerform(position.x, position.y, position.z);
        }
    }

    private static Vector3Int GetPositionFromIndex(ChunkData chunkData, int index)
    {
        int x = index % chunkData.chunkSize;
        int y = (index / chunkData.chunkSize) % chunkData.chunkHeight;
        int z = index / (chunkData.chunkSize * chunkData.chunkHeight);
        return new Vector3Int(x, y, z);
    }

    private static bool InRangeXZ(ChunkData chunkData, int axisCoordinate)
    {
        return !(axisCoordinate < 0 || axisCoordinate >= chunkData.chunkSize);
    }

    private static bool InRangeY(ChunkData chunkData, int yCoordinate)
    {
        return !(yCoordinate < 0 || yCoordinate >= chunkData.chunkHeight);
    }

    public static BlockType GetBlockFromCoordinatesInChunk(ChunkData chunkData, Vector3Int coordinatesInChunk)
    {
        return GetBlockFromCoordinatesInChunk(chunkData, coordinatesInChunk.x, coordinatesInChunk.y, coordinatesInChunk.z);
    }

    public static BlockType GetBlockFromCoordinatesInChunk(ChunkData chunkData, int x, int y, int z)
    {
        if (InRangeXZ(chunkData, x) && InRangeXZ(chunkData, z) && InRangeY(chunkData, y))
        {
            return chunkData.blocks[GetIndexFromCoordinatesInChunk(chunkData, x, y, z)];
        }

        return chunkData.worldReference.GetBlockFromChunkCoordinates(chunkData, chunkData.worldPosition.x + x, chunkData.worldPosition.y + y, chunkData.worldPosition.z + z);
    }

    public static void SetBlock(ChunkData chunkData, Vector3Int localPosition, BlockType block)
    {
        if (InRangeXZ(chunkData, localPosition.x) && InRangeY(chunkData, localPosition.y) && InRangeXZ(chunkData, localPosition.z))
        {
            int index = GetIndexFromCoordinatesInChunk(chunkData, localPosition.x, localPosition.y, localPosition.z);
            chunkData.blocks[index] = block;
        }
        else
        {
            throw new Exception("Need to ask World for appropriate chunk");
        }
    }

    private static int GetIndexFromCoordinatesInChunk(ChunkData chunkData, int x, int y, int z)
    {
        return x + chunkData.chunkSize * y + chunkData.chunkSize * chunkData.chunkHeight * z;
    }

    public static Vector3Int GetBlockInChunkCoordinates(ChunkData chunkData, Vector3Int pos)
    {
        return new Vector3Int
        {
            x = pos.x - chunkData.worldPosition.x,
            y = pos.y - chunkData.worldPosition.y,
            z = pos.z - chunkData.worldPosition.z
        };
    }

    public static MeshData GetChunkMeshData(ChunkData chunkData)
    {
        MeshData meshData = new MeshData(true);

        LoopThroughTheBlocks(chunkData, (x, y, z) => meshData = BlockHelper.GetMeshData(chunkData, x, y, z, meshData, chunkData.blocks[GetIndexFromCoordinatesInChunk(chunkData, x, y, z)]));

        return meshData;
    }

    internal static Vector3Int ChunkPositionFromBlockCoords(World world, int x, int y, int z)
    {
        Vector3Int pos = new Vector3Int
        {
            x = Mathf.FloorToInt(x / (float)world.chunkSize) * world.chunkSize,
            y = Mathf.FloorToInt(y / (float)world.chunkHeight) * world.chunkHeight,
            z = Mathf.FloorToInt(z / (float)world.chunkSize) * world.chunkSize
        };
        return pos;
    }

    internal static Vector3Int ChunkPositionFromBlockCoords(World world, Vector3Int position)
    {
        return ChunkPositionFromBlockCoords((World)world, position.x, position.y, position.z);
    }
}