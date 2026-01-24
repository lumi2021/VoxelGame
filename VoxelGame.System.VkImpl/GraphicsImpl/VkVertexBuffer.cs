using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using VoxelGame.Core.Data.Graphics;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VoxelGame.Engine.GraphicsImpl;

internal unsafe class VkVertexBuffer<T> : IVertexBuffer<T>, IVkGenericVertexBuffer, IDisposable where T : struct
{
    private Buffer _buf;
    private DeviceMemory _mem;
    public Buffer Buffer => _buf;
    
    internal VkVertexBuffer()
    {
        var bufferInfo = new BufferCreateInfo()
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)(Unsafe.SizeOf<T>() * 64),
            Usage = BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit,
            SharingMode = SharingMode.Exclusive
        };

        if (Vulkan.Vk.CreateBuffer(Vulkan.Device, &bufferInfo, null, out _buf) != Result.Success)
            throw new Exception("Error creating vertex buffer");
    }
    
    public void Fetch(T[] indices)
    {
        var vk = Vulkan.Vk;
        var dev = Vulkan.Device;
        ulong bytesSize = (uint)(Unsafe.SizeOf<T>() * Math.Max(64, indices.Length));
        
        // Create destiny buffer in memory
        var newBufferInfo = new BufferCreateInfo() {
            SType = StructureType.BufferCreateInfo,
            Size = bytesSize,
            Usage = BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit,
            SharingMode = SharingMode.Exclusive
        };
        if (Vulkan.Vk.CreateBuffer(Vulkan.Device, &newBufferInfo, null, out var dstBuffer) != Result.Success)
            throw new Exception("Error creating vertex buffer");
        
        vk.GetBufferMemoryRequirements(dev, dstBuffer, out var dstMemRequirements);
        var dstAllocInfo = new MemoryAllocateInfo {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = dstMemRequirements.Size,
            MemoryTypeIndex = Vulkan.FindMemoryType(dstMemRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit),
        };
        vk.AllocateMemory(dev, &dstAllocInfo, null, out var dstBufferMemory);
        vk.BindBufferMemory(dev, dstBuffer, dstBufferMemory, 0);
        
        
        // Create source buffer in memory
        var srcBufferInfo = new BufferCreateInfo() {
            SType = StructureType.BufferCreateInfo,
            Size = bytesSize,
            Usage = BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferSrcBit,
            SharingMode = SharingMode.Exclusive
        };
        if (vk.CreateBuffer(dev, &srcBufferInfo, null, out var srcBuffer) != Result.Success)
            throw new Exception("Error creating vertex buffer");
        
        vk.GetBufferMemoryRequirements(dev, srcBuffer, out var memRequirements);
        var srcAllocInfo = new MemoryAllocateInfo {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = Vulkan.FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit),
        };
        vk.AllocateMemory(dev, &srcAllocInfo, null, out var srcBufferMemory);
        vk.BindBufferMemory(dev, srcBuffer, srcBufferMemory, 0);
        
        
        // Maps source memory and copy data to it
        void* mapped;
        vk.MapMemory(dev, srcBufferMemory, 0, bytesSize, 0, &mapped);
        indices.CopyTo(new Span<T>(mapped, indices.Length));
        vk.UnmapMemory(dev, srcBufferMemory);
        
        var cmd = Vulkan.BeginSingleTimeCommands();
        var copyRegion = new BufferCopy { SrcOffset = 0, DstOffset = 0, Size = bytesSize };
        vk.CmdCopyBuffer(cmd, srcBuffer, dstBuffer, 1, &copyRegion);
        Vulkan.EndSingleTimeCommands(cmd);
        
        vk.FreeMemory(dev, srcBufferMemory, null);
        vk.DestroyBuffer(dev, srcBuffer, null);
        
        vk.FreeMemory(dev, _mem, null);
        vk.DestroyBuffer(dev, _buf, null);
        
        _buf = dstBuffer;
        _mem = dstBufferMemory;
    }
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
        Vulkan.Vk.FreeMemory(Vulkan.Device, _mem, null);
        Vulkan.Vk.DestroyBuffer(Vulkan.Device, _buf, null);
    }

    public override string ToString() => $"{_buf.Handle:x16}";
}
