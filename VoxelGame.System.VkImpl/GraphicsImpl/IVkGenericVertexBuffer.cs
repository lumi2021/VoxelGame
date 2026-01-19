using VoxelGame.Core.Data.Graphics;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VoxelGame.Engine.GraphicsImpl;

internal interface IVkGenericVertexBuffer : IGenericVertexBuffer
{
    public Buffer Buffer { get; }
}