using System.Numerics;
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
    public IMaterial GenerateMaterial(string vertPath, string fragPath,
        MaterialType[] attrTypes, MaterialType[] vertexUniforms, MaterialType[] fragmentUniforms);
    
    public void BindMaterial(IMaterial material);
    public void BindMesh(IIndexBuffer indexBuffer, IGenericVertexBuffer[] vertexBuffers);
    public void BindMat4(int index, Matrix4x4 matrix);
    public void Draw();
}
