using VoxelGame.Core.Data.Graphics;

namespace VoxelGame.Core;

public interface IGraphics
{
 
    public void Init();
    public void CleanUp();
    
    public void BeginRenderingFrame();
    public void EndRenderingFrame();
    
    public IIndexBuffer GenerateIndexBuffer();
    public IVertexBuffer<T> GenerateVertexBuffer<T>() where T : struct;
    public IMaterial GenerateMaterial(string vertPath, string fragPath, MaterialAttributeType[] attrTypes);
    
    public void BindMaterial(IMaterial material);
    public void BindMesh(IIndexBuffer ibuf, IGenericVertexBuffer[] vbufs);
    public void Draw();
}
