using Silk.NET.Vulkan;
using VoxelGame.Core;
using VoxelGame.Core.Data.Graphics;
using VoxelGame.Engine.GraphicsImpl;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VoxelGame.Engine;

public unsafe class VkGraphics : IGraphics
{
    private IMaterial? _lastMaterial;
    private uint _lastIndexCount = 0;
    private uint _lastInstanceCount = 1;
    
    public void Init() => Vulkan.Init();
    public void CleanUp() => Vulkan.CleanUp();

    public void BeginRenderingFrame()
    {
        Vulkan.BeginRenderingFrame();
        _lastMaterial = null;
    }
    public void EndRenderingFrame() => Vulkan.EndRenderingFrame();

    public IIndexBuffer GenerateIndexBuffer() => new VkIndexBuffer();
    public IVertexBuffer<T> GenerateVertexBuffer<T>() where T : struct => new VkVertexBuffer<T>();

    public IMaterial GenerateMaterial(string vertPath, string fragPath, MaterialAttributeType[] attrTypes)
        => new VkMaterial(vertPath, fragPath, attrTypes);

    public void BindMaterial(IMaterial material)
    {
        if (_lastMaterial == material) return;
        var vk = Vulkan.Vk;
        vk.CmdBindPipeline(Vulkan.CurrentCommandBuffer, PipelineBindPoint.Graphics, ((VkMaterial)material).GraphicsPipeline);
        _lastMaterial = material;
    }

    public void BindMesh(IIndexBuffer ibuf, IGenericVertexBuffer[] vbufs)
    {
        var vk = Vulkan.Vk;
        var cmd = Vulkan.CurrentCommandBuffer;
        var indexBuffer = ((VkIndexBuffer)ibuf);
        
        var buffers = stackalloc Buffer[vbufs.Length];
        var offsets = stackalloc ulong[vbufs.Length];

        foreach (var (i, v) in vbufs.Index())
        {
            buffers[i] = ((IVkGenericVertexBuffer)v).Buffer;
            offsets[i] = 0;
        }
        
        vk.CmdBindVertexBuffers(cmd, 0, (uint)vbufs.Length, buffers, offsets);
        vk.CmdBindIndexBuffer(cmd, indexBuffer.Buffer, 0, IndexType.Uint16);

        _lastIndexCount = (uint)indexBuffer.Size;
    }

    public void Draw() => Vulkan.Vk.CmdDrawIndexed(Vulkan.CurrentCommandBuffer,
        _lastIndexCount,
        _lastInstanceCount,
        0,
        0,
        0);
}
