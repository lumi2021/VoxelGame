using System.Numerics;
using Silk.NET.Vulkan;
using VoxelGame.Core;
using VoxelGame.Core.Data.Graphics;
using VoxelGame.Engine.GraphicsImpl;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VoxelGame.Engine;

public unsafe class VkGraphics : IGraphics
{
    private IMaterial? _lastMaterial;
    private PipelineLayout _lastPipelineLayout = default;
    private uint _lastIndexCount = 0;
    private uint _lastInstanceCount = 1;
    
    public void Init() => Vulkan.Init();
    public void Resize(int width, int height) => Vulkan.Resize(width, height);
    public void CleanUp() => Vulkan.CleanUp();

    public void BeginRenderingFrame()
    {
        Vulkan.BeginRenderingFrame();
        _lastMaterial = null;
    }
    public void EndRenderingFrame() => Vulkan.EndRenderingFrame();

    public IIndexBuffer GenerateIndexBuffer() => new VkIndexBuffer();
    public IVertexBuffer<T> GenerateVertexBuffer<T>() where T : struct => new VkVertexBuffer<T>();
    public ITexture GenerateTexture(string filePath) => VkTexture.FromFile(filePath);
    
    public IMaterial GenerateMaterial(string vertPath, string fragPath, MaterialType[] a, MaterialType[] v, MaterialType[] f, uint t)
        => new VkMaterial(vertPath, fragPath, a, v, f, t);

    public void BindMaterial(IMaterial material)
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
        _lastPipelineLayout = mat.PipelineLayout;
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
        vk.CmdBindIndexBuffer(cmd, indexBuffer.Buffer, 0, IndexType.Uint32);

        _lastIndexCount = (uint)indexBuffer.Size;
    }

    public void BindMat4(int index, Matrix4x4 matrix)
    {
        var offset = (uint)(sizeof(Matrix4x4) * index);
        var length = (uint)sizeof(Matrix4x4);
        Vulkan.Vk.CmdPushConstants(
            Vulkan.CurrentCommandBuffer,
            _lastPipelineLayout,
            ShaderStageFlags.VertexBit,
            offset, length, ref matrix);
    }
    
    public void Draw() => Vulkan.Vk.CmdDrawIndexed(Vulkan.CurrentCommandBuffer,
        _lastIndexCount, _lastInstanceCount, 0, 0, 0);
}
