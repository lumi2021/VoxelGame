namespace VoxelGame.Core.Data.Graphics;

public interface IVertexBuffer<in T> : IGenericVertexBuffer where T : struct
{
 
    public void Fetch(T[] indices);
    
}
