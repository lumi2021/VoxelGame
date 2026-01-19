using System.Diagnostics;
using System.Numerics;
using VoxelGame.Core.Data;

namespace VoxelGame.Core.World;

public class Chunk
{
     public Vector3 ChunkPosition = default;

     public uint SizeX => Data.SizeX;
     public uint SizeY => Data.SizeY;
     public uint SizeZ => Data.SizeZ;
     
     public ChunkData Data;

     public Chunk(uint sizeX, uint sizeY, uint sizeZ)
     {
          Data = new ChunkData(sizeX, sizeY, sizeZ);
          Debug.WriteLine($"Created chunk with dimensions {sizeX}x{sizeY}x{sizeZ}");
     }

     public void BuildMesh()
     {
          
     }
     
     public void Draw()
     {
          
     }
}
