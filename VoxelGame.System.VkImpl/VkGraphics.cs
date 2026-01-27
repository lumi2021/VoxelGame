using Silk.NET.Vulkan;
using VoxelGame.Core;
using VoxelGame.Core.Data.Graphics;
using VoxelGame.Core.Math;
using VoxelGame.Engine.GraphicsImpl;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VoxelGame.Engine;

public unsafe class VkGraphics : IGraphics
{
    private IMaterial? _lastMaterial;
    private readonly VkRenderContext _renderContext;
    
    internal bool InFrame { get; private set; } = false;
    public IRenderContext Context
    {
        get
        {
            _renderContext.Reset();
            return _renderContext;
        }
    }

    public Vec2U ViewportSize => new Vec2U(Vulkan.ViewportExtent.Width, Vulkan.ViewportExtent.Height);

    public VkGraphics()
    {
        _renderContext = new VkRenderContext(this);
    }
    
    public void Init() => Vulkan.Init();
    public void Resize(int width, int height) => Vulkan.Resize();
    public void CleanUp() => Vulkan.CleanUp();

    public void BeginRenderingFrame()
    {
        Vulkan.BeginRenderingFrame();
        InFrame = true;
        _lastMaterial = null;
    }
    public void EndRenderingFrame()
    {
        InFrame = false;
        Vulkan.EndRenderingFrame();
    }

    public IIndexBuffer GenerateIndexBuffer() => new VkIndexBuffer();
    public IVertexBuffer<T> GenerateVertexBuffer<T>() where T : struct => new VkVertexBuffer<T>();
    public ITexture GenerateTexture(string filePath) => VkTexture.FromFile(filePath);

    public IMaterial GenerateMaterial(MaterialOptions options) => new VkMaterial(options);

    internal void BindMaterial(IMaterial material)
    {
        if (_lastMaterial == material) return;
        
        var vk = Vulkan.Vk;
        var mat = (VkMaterial)material;
        var dst = mat.DescriptorSet;
        
        vk.CmdBindPipeline(Vulkan.CurrentCommandBuffer, PipelineBindPoint.Graphics, mat.GraphicsPipeline);
        if (dst.HasValue)
        {
            var descriptorSet = dst.Value;
            vk.CmdBindDescriptorSets(
                Vulkan.CurrentCommandBuffer,
                PipelineBindPoint.Graphics,
                mat.PipelineLayout,
                0,
                1,
                &descriptorSet,
                0,
                null
                );
        }
        
        _lastMaterial = material;
    }

    internal void BindMesh(IIndexBuffer ibuf, IGenericVertexBuffer[] vbufs)
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
        vk.CmdBindIndexBuffer(cmd, indexBuffer.Buffer, 0, IndexType.Uint32);
    }
    

    internal void DrawIndexed(uint indexCount, uint instanceCount)
        => Vulkan.Vk.CmdDrawIndexed(Vulkan.CurrentCommandBuffer, indexCount, instanceCount, 0, 0, 0);
}
