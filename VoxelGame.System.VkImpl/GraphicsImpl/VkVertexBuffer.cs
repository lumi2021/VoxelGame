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
            Size = (ulong)(sizeof(T) * 64),
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
        ulong bytesSize = (uint)(sizeof(T) * indices.Length);
        
        var bufferInfo = new BufferCreateInfo()
        {
            SType = StructureType.BufferCreateInfo,
            Size = bytesSize,
            Usage = BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferSrcBit,
            SharingMode = SharingMode.Exclusive
        };

        if (vk.CreateBuffer(dev, &bufferInfo, null, out var srcBuffer) != Result.Success)
            throw new Exception("Error creating index buffer");

        vk.GetBufferMemoryRequirements(dev, srcBuffer, out var memRequirements);
        var srcAllocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = Vulkan.FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit),
        };
        vk.AllocateMemory(dev, &srcAllocInfo, null, out var bufferMemory);
        vk.BindBufferMemory(dev, srcBuffer, bufferMemory, 0);
        
        void* mapped;
        vk.MapMemory(dev, bufferMemory, 0, bytesSize, 0, &mapped);
        indices.CopyTo(new Span<T>(mapped, indices.Length));
        vk.UnmapMemory(dev, bufferMemory);
        
        var oldMem = _mem;
        vk.GetBufferMemoryRequirements(dev, _buf, out memRequirements);
        var dstAllocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = Vulkan.FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit),
        };
        vk.AllocateMemory(dev, &dstAllocInfo, null, out _mem);
        vk.BindBufferMemory(dev, _buf, _mem, 0);
        
        var cmd = Vulkan.BeginSingleTimeCommands();
        
        var copyRegion = new BufferCopy { SrcOffset = 0, DstOffset = 0, Size = bytesSize };
        
        vk.CmdCopyBuffer(cmd, srcBuffer, _buf, 1, &copyRegion);
        Vulkan.EndSingleTimeCommands(cmd);
        
        vk.FreeMemory(dev, bufferMemory, null);
        vk.FreeMemory(dev, oldMem, null);
    }
    public void Bind()
    {
        throw new NotImplementedException();
    }
    
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
        Vulkan.Vk.FreeMemory(Vulkan.Device, _mem, null);
        Vulkan.Vk.DestroyBuffer(Vulkan.Device, _buf, null);
    }
}
