using ImageMagick;
using Silk.NET.Vulkan;
using VoxelGame.Core.Data.Graphics;

namespace VoxelGame.Engine.GraphicsImpl;

public unsafe class VkTexture : ITexture, IDisposable
{
    
    private DeviceMemory _mem;
    private Image _img;
    internal ImageView ImageView;
    internal Sampler ImageSampler;
    
    private VkTexture() {}
    
    public static ITexture FromFile(string file)
    {
        var img = new MagickImage(file);
        var pixels = img.GetPixels().ToByteArray(PixelMapping.RGBA);

        var tex = new VkTexture();
        tex.FetchBytes(img.Width, img.Height, pixels!);
        return tex;
    }

    private void FetchBytes(uint width, uint height, byte[] bytes)
    {
        var vk = Vulkan.Vk;
        var dev = Vulkan.Device;
        
        // Create source buffer in memory
        var srcBufferInfo = new BufferCreateInfo() {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)bytes.Length,
            Usage = BufferUsageFlags.TransferSrcBit,
            SharingMode = SharingMode.Exclusive
        };
        if (vk.CreateBuffer(dev, &srcBufferInfo, null, out var srcBuffer) != Result.Success)
            throw new Exception("Error creating staging buffer");
        
        vk.GetBufferMemoryRequirements(dev, srcBuffer, out var memRequirements);
        var srcAllocInfo = new MemoryAllocateInfo {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = Vulkan.FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit),
        };
        vk.AllocateMemory(dev, &srcAllocInfo, null, out var srcBufferMemory);
        vk.BindBufferMemory(dev, srcBuffer, srcBufferMemory, 0);
        
        // Create image and allocating memory
        var imageCreateInfo = new ImageCreateInfo()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = Format.R8G8B8A8Srgb,
            Extent =
            {
                Width = width,
                Height = height,
                Depth = 1,
            },
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            SharingMode =  SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined,
        };
        if (vk.CreateImage(dev, imageCreateInfo, null, out var dstImage) != Result.Success)
            throw new Exception("Error creating image");
        
        vk.GetImageMemoryRequirements(dev, dstImage, out var imageRequirements);
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = imageRequirements.Size,
            MemoryTypeIndex =
                Vulkan.FindMemoryType(imageRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit),
        };
        
        vk.AllocateMemory(dev, &allocInfo, null, out var dstImageMemory);
        vk.BindImageMemory(dev, dstImage, dstImageMemory, 0);
        
        // Maps source memory and copy data to it
        void* mapped;
        vk.MapMemory(dev, srcBufferMemory, 0, (ulong)bytes.Length, 0, &mapped);
        bytes.CopyTo(new Span<byte>(mapped, bytes.Length));
        vk.UnmapMemory(dev, srcBufferMemory);
        
        var cmd = Vulkan.BeginSingleTimeCommands();
        BufferImageCopy[] copyRegions = [
            new() {
                BufferRowLength = width,
                BufferImageHeight = height,
                BufferOffset = 0,
                ImageExtent = { Width = width, Height = height, Depth = 1 },
                ImageOffset = { X = 0, Y = 0, Z = 0 },
                ImageSubresource =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            },
        ];
        vk.CmdCopyBufferToImage(cmd, srcBuffer, dstImage, ImageLayout.TransferDstOptimal, copyRegions.AsSpan());
        Vulkan.EndSingleTimeCommands(cmd);

        vk.FreeMemory(dev, srcBufferMemory, null);
        vk.DestroyBuffer(dev, srcBuffer, null);
        
        if (ImageView.Handle != 0x0) vk.DestroyImageView(dev, ImageView, null);
        if (ImageSampler.Handle != 0x0) vk.DestroySampler(dev, ImageSampler, null);
        if (_mem.Handle != 0x0) vk.FreeMemory(dev, _mem, null);
        if (_img.Handle != 0x0) vk.DestroyImage(dev, _img, null);
        
        _img = dstImage;
        _mem = dstImageMemory;
        
        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _img,
            ViewType = ImageViewType.Type2D,
            Format = Format.R8G8B8A8Srgb,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };
        vk.CreateImageView(dev, &viewInfo, null, out ImageView);
        
        var samplerInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Nearest,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            MinLod = 0,
            MaxLod = 0,
        };
        vk.CreateSampler(dev, &samplerInfo, null, out ImageSampler);
    }

    
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        var vk = Vulkan.Vk;
        var dev = Vulkan.Device;
        
        if (ImageView.Handle != 0x0) vk.DestroyImageView(dev, ImageView, null);
        if (ImageSampler.Handle != 0x0) vk.DestroySampler(dev, ImageSampler, null);
        if (_mem.Handle != 0x0) vk.FreeMemory(dev, _mem, null);
        if (_img.Handle != 0x0) vk.DestroyImage(dev, _img, null);
    }
}
