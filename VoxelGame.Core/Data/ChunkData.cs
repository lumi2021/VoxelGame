using System.Diagnostics;

namespace VoxelGame.Core.Data;

public readonly struct ChunkData
{
     public readonly uint SizeX;
     public readonly uint SizeY;
     public readonly uint SizeZ;
     public readonly byte[] Data;

     public byte this[int x, int y, int z]
     {
          get => Data[x + (z * SizeX) + (y * SizeX * SizeZ)];
          set => Data[x + (z * SizeX) + (y * SizeX * SizeZ)] = value;
     }

     public ChunkData(uint sizeX, uint sizeY, uint sizeZ)
     {
          SizeX = sizeX;
          SizeY = sizeY;
          SizeZ = sizeZ;
          
          Data = GC.AllocateUninitializedArray<byte>((int)(sizeX * sizeY * sizeZ));
          
          Debug.WriteLine($"Created chunk with dimensions {sizeX}x{sizeY}x{sizeZ}");
     }


     public bool TryGetValue(int x, int y, int z, out byte value)
     {
          value = 0;
          if (x < 0 || x >= SizeX || y < 0 || y >= SizeY || z < 0 || z >= SizeZ) return false;
          value = this[x, y, z];
          return true;
     }
}
