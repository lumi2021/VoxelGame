using System.Numerics;
using VoxelGame.Core.Data.Graphics;

namespace VoxelGame.Engine.GraphicsImpl;

internal class VkRenderContext(VkGraphics graphics) : IRenderContext
{
    private VkMaterial _material = null!;
    private ulong _indexCount;
    private ulong _instanceCount;

    public IRenderContext WithMaterial(IMaterial material)
    {
        _material = (VkMaterial)material;
        graphics.BindMaterial(_material);
        return this;
    }
    
    public IRenderContext WithMesh(IIndexBuffer indices, IGenericVertexBuffer[] vertexAttributes)
    {
        graphics.BindMesh(indices, vertexAttributes);
        _indexCount = ((VkIndexBuffer)indices).Size;
        _instanceCount = 1;
        
        return this;
    }
    
    public IRenderContext WithTexture(uint index, ITexture texture)
    {
        _material.UseTexture(index, texture);
        return this;
    }
    
    public IRenderContext WithVertexUniform(uint index, Matrix4x4 matrix)
    {
        _material.BindVertexUniform(index, matrix);
        return this;
    }

    public void Reset()
    {
        _material = null!;
        _indexCount = 0;
        _instanceCount = 0;
    }
    public void Draw()
    {
        if (!graphics.InFrame) throw new Exception("Cannot draw graphics outside of rendering frame");
        graphics.DrawIndexed((uint)_indexCount, (uint)_instanceCount);
        _material = null!;
    }
}
