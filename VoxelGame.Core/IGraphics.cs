using VoxelGame.Core.Data.Graphics;

namespace VoxelGame.Core;

public interface IGraphics
{
 
    public void Init();
    public void Resize(int width, int height);
    public void CleanUp();
    
    public void BeginRenderingFrame();
    public void EndRenderingFrame();
    
    public IIndexBuffer GenerateIndexBuffer();
    public IVertexBuffer<T> GenerateVertexBuffer<T>() where T : struct;
    public IMaterial GenerateMaterial(
        string vertPath, string fragPath,
        MaterialType[] at,
        MaterialType[] vu,
        MaterialType[] fu,
        uint tc);
    public ITexture GenerateTexture(string filePath);
    
    public void BindMaterial(IMaterial material);
    public void BindMesh(IIndexBuffer indexBuffer, IGenericVertexBuffer[] vertexBuffers);
    public void BindMat4(int index, Mat4 matrix);
    public void Draw();
}
