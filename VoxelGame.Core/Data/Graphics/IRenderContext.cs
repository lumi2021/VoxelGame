namespace VoxelGame.Core.Data.Graphics;

public interface IRenderContext
{
    public IRenderContext WithMaterial(IMaterial material);
    
    public IRenderContext WithVertexUniform(uint index, Mat4 matrix);
    public IRenderContext WithTexture(uint index, ITexture texture);
    
    public IRenderContext WithMesh(IIndexBuffer indices, IGenericVertexBuffer[] attributes);

    public void Reset();
    public void Draw();

}
