using VoxelGame.Core.Data.Graphics;

namespace VoxelGame.Core;

public interface IGraphics
{
    public IRenderContext Context { get; }
    public Vec2U ViewportSize { get; }
    
    public IIndexBuffer GenerateIndexBuffer();
    public IVertexBuffer<T> GenerateVertexBuffer<T>() where T : struct;
    public IMaterial GenerateMaterial(MaterialOptions options);
    public ITexture GenerateTexture(string filePath);
    
}
