using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using VoxelGame.Core.Data.Graphics;

namespace VoxelGame.Engine.GraphicsImpl;

public unsafe class VkMaterial : IMaterial, IDisposable
{
    
    private static Pipeline _graphicsPipeline;
    private PipelineLayout _pipelineLayout;
    private RenderPass _renderPass;
    
    public Pipeline GraphicsPipeline => _graphicsPipeline;
    public RenderPass RenderPass => _renderPass;
    
    internal VkMaterial(string vertPath, string fragPath, MaterialAttributeType[] attrType)
    {
        var vk = Vulkan.Vk;
        var dev = Vulkan.Device;
        
        var vertShaderCode = File.ReadAllBytes(vertPath);
        var fragShaderCode = File.ReadAllBytes(fragPath);

        var vertShaderModule = CreateShaderModule(vertShaderCode);
        var fragShaderModule = CreateShaderModule(fragShaderCode);

        PipelineShaderStageCreateInfo vertShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        PipelineShaderStageCreateInfo fragShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var shaderStages = stackalloc[] { vertShaderStageInfo, fragShaderStageInfo };

        List<VertexInputBindingDescription> bindings = [];
        List<VertexInputAttributeDescription> attributes = [];

        for (uint i = 0; i < attrType.Length; i++)
        {
            if (attrType[i] == MaterialAttributeType.Void) continue;
            
            var stride = (uint)(attrType[i] switch
            {
                MaterialAttributeType.Vec2 => sizeof(float) * 2,
                MaterialAttributeType.Vec3 => sizeof(float) * 3,
                MaterialAttributeType.Vec4 => sizeof(float) * 4,
                MaterialAttributeType.Float => sizeof(float),
                MaterialAttributeType.Int => sizeof(int),
                MaterialAttributeType.UInt => sizeof(uint),
                _ => throw new ArgumentOutOfRangeException()
            });
            var format = attrType[i] switch
            {
                MaterialAttributeType.Vec2 => Format.R32G32Sfloat,
                MaterialAttributeType.Vec3 => Format.R32G32B32Sfloat,
                MaterialAttributeType.Vec4 => Format.R32G32B32A32Sfloat,
                MaterialAttributeType.Float => Format.R32Sfloat,
                MaterialAttributeType.Int => Format.R32Sint,
                MaterialAttributeType.UInt => Format.R32Uint,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            bindings.Add(new VertexInputBindingDescription { Binding = i, Stride = stride, InputRate = VertexInputRate.Vertex});
            attributes.Add(new VertexInputAttributeDescription { Binding = i, Location = i, Format = format, Offset = 0 });
        }

        var bindArr = bindings.ToArray();
        var attrArr = attributes.ToArray();

        PipelineVertexInputStateCreateInfo vertexInputInfo;
        fixed (VertexInputBindingDescription* pBind = bindArr)
        fixed (VertexInputAttributeDescription* pAttr = attrArr)
        {
            vertexInputInfo = new PipelineVertexInputStateCreateInfo
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                PVertexBindingDescriptions = pBind,
                PVertexAttributeDescriptions = pAttr,
                VertexBindingDescriptionCount = 2,
                VertexAttributeDescriptionCount = 2,
            };
        }

        PipelineInputAssemblyStateCreateInfo inputAssembly = new()
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false,
        };

        Viewport viewport = new()
        {
            X = 0,
            Y = 0,
            Width = Vulkan.SwapChainExtent.Width,
            Height = Vulkan.SwapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };

        Rect2D scissor = new()
        {
            Offset = { X = 0, Y = 0 },
            Extent = Vulkan.SwapChainExtent,
        };

        PipelineViewportStateCreateInfo viewportState = new()
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor,
        };

        PipelineRasterizationStateCreateInfo rasterizer = new()
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = false,
        };

        PipelineMultisampleStateCreateInfo multisampling = new()
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
        };

        PipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false,
        };

        PipelineColorBlendStateCreateInfo colorBlending = new()
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment,
        };

        colorBlending.BlendConstants[0] = 0;
        colorBlending.BlendConstants[1] = 0;
        colorBlending.BlendConstants[2] = 0;
        colorBlending.BlendConstants[3] = 0;

        PipelineLayoutCreateInfo pipelineLayoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 0,
            PushConstantRangeCount = 0,
        };

        if (vk.CreatePipelineLayout(dev, in pipelineLayoutInfo, null, out _pipelineLayout) != Result.Success)
            throw new Exception("failed to create pipeline layout!");

        CreateRenderPass();
        
        GraphicsPipelineCreateInfo pipelineInfo = new()
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PStages = shaderStages,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PColorBlendState = &colorBlending,
            Layout = _pipelineLayout,
            RenderPass = _renderPass,
            Subpass = 0,
            BasePipelineHandle = default
        };

        var res = vk.CreateGraphicsPipelines(dev, default, 1, in pipelineInfo,
            null, out _graphicsPipeline); 
        if (res != Result.Success) throw new Exception("failed to create graphics pipeline!");
        
        vk.DestroyShaderModule(dev, fragShaderModule, null);
        vk.DestroyShaderModule(dev, vertShaderModule, null);

        SilkMarshal.Free((nint)vertShaderStageInfo.PName);
        SilkMarshal.Free((nint)fragShaderStageInfo.PName);
    }
    private static ShaderModule CreateShaderModule(byte[] code)
    {
        ShaderModuleCreateInfo createInfo = new()
        {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint)code.Length,
        };

        ShaderModule shaderModule;
        fixed (byte* codePtr = code)
        {
            createInfo.PCode = (uint*)codePtr;

            if (Vulkan.Vk.CreateShaderModule(Vulkan.Device, in createInfo, null, out shaderModule) != Result.Success)
                throw new Exception();
        }
        return shaderModule;

    }
    private void CreateRenderPass()
    {
        AttachmentDescription colorAttachment = new()
        {
            Format = Vulkan.SwapChainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr,
        };

        AttachmentReference colorAttachmentRef = new()
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal,
        };

        SubpassDescription subpass = new()
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
        };

        SubpassDependency dependency = new()
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit
        };

        RenderPassCreateInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency,
        };

        if (Vulkan.Vk.CreateRenderPass(Vulkan.Device, in renderPassInfo, null, out _renderPass) != Result.Success)
            throw new Exception("failed to create render pass!");
    }
    
    public void Bind()
    {
        throw new NotImplementedException();
    }

    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
        var vk = Vulkan.Vk;
        var dev = Vulkan.Device;
        
        vk.DestroyPipeline(dev, _graphicsPipeline, null);
        vk.DestroyPipelineLayout(dev, _pipelineLayout, null);
        vk.DestroyRenderPass(dev, _renderPass, null);
    }
}
