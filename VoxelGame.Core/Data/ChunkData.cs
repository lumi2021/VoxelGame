using System.Diagnostics;

namespace VoxelGame.Core.Data;

public readonly struct ChunkData
{
     public readonly uint SizeX;
     public readonly uint SizeY;
     public readonly uint SizeZ;
     public readonly bool[] Data;
     
     public bool this[int x, int y, int z] => Data[y * SizeX + z * SizeX + x];

     public ChunkData(uint sizeX, uint sizeY, uint sizeZ)
     {
          SizeX = sizeX;
          SizeY = sizeY;
          SizeZ = sizeZ;
          
          Data = GC.AllocateUninitializedArray<bool>((int)(sizeX * sizeY * sizeZ));
          
          Debug.WriteLine($"Created chunk with dimensions {sizeX}x{sizeY}x{sizeZ}");
     }
}
