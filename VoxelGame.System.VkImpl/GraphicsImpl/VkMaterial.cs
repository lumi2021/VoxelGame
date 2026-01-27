using System.Diagnostics;
using System.Numerics;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using VoxelGame.Core.Data.Graphics;

namespace VoxelGame.Engine.GraphicsImpl;

public unsafe class VkMaterial : IMaterial, IDisposable
{
    
    private Pipeline _graphicsPipeline;
    private PipelineLayout _pipelineLayout;

    private DescriptorSet _descriptorSet;
    private DescriptorPool _descriptorPool;

    private readonly MaterialType[] _vertexAttributes;
    private readonly (uint off, MaterialType ty)[] _vertexUniforms;
    private readonly (uint off, MaterialType ty)[] _fragmentUniforms;
    private readonly uint _textureCount;
    
    public DescriptorSet? DescriptorSet => _descriptorSet.Handle == 0x0 ? null : _descriptorSet;
    
    public Pipeline GraphicsPipeline => _graphicsPipeline;
    public PipelineLayout PipelineLayout => _pipelineLayout;
    
    internal VkMaterial(MaterialOptions options)
    {
        var vk = Vulkan.Vk;
        var dev = Vulkan.Device;
        
        var vertShaderCode = File.ReadAllBytes(options.VertShaderPath);
        var fragShaderCode = File.ReadAllBytes(options.FragShaderPath);

        var vertShaderModule = CreateShaderModule(vertShaderCode);
        var fragShaderModule = CreateShaderModule(fragShaderCode);

        var entryPointName = (byte*)SilkMarshal.StringToPtr("main");
        
        PipelineShaderStageCreateInfo vertShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = entryPointName,
        };

        PipelineShaderStageCreateInfo fragShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = entryPointName,
        };

        var shaderStages = stackalloc[] { vertShaderStageInfo, fragShaderStageInfo };

        List<VertexInputBindingDescription> bindings = [];
        List<VertexInputAttributeDescription> attributes = [];

        _vertexAttributes = options.VertAttributes.ToArray();
        for (uint i = 0; i < options.VertAttributes.Length; i++)
        {
            if (_vertexAttributes[i] == MaterialType.Void) continue;
            
            var format = _vertexAttributes[i] switch
            {
                MaterialType.Vec2 => Format.R32G32Sfloat,
                MaterialType.Vec3 => Format.R32G32B32Sfloat,
                MaterialType.Vec4 => Format.R32G32B32A32Sfloat,
                MaterialType.Float => Format.R32Sfloat,
                MaterialType.Int => Format.R32Sint,
                MaterialType.UInt => Format.R32Uint,
                _ => throw new UnreachableException()
            };
            var stride = SizeOf(_vertexAttributes[i]);
            
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
                VertexBindingDescriptionCount = (uint)bindArr.Length,
                PVertexAttributeDescriptions = pAttr,
                VertexAttributeDescriptionCount = (uint)attrArr.Length,
            };
        }

        PipelineInputAssemblyStateCreateInfo inputAssembly = new()
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false,
        };
        
        PipelineViewportStateCreateInfo viewportState = new()
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount = 1,
            PViewports = null,
            PScissors = null,
        };

        PipelineRasterizationStateCreateInfo rasterizer = new()
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            LineWidth = 1,
            PolygonMode = options.GeometryMode switch
            {
                MaterialOptions.GeometryModes.Points => PolygonMode.Point,
                MaterialOptions.GeometryModes.Lines => PolygonMode.Line,
                MaterialOptions.GeometryModes.Triangles => PolygonMode.Fill,
                _ => throw new ArgumentOutOfRangeException()
            },
            CullMode = options.CullFaceMode switch
            {
                MaterialOptions.CullFaceModes.Front => CullModeFlags.FrontBit,
                MaterialOptions.CullFaceModes.Back => CullModeFlags.BackBit,
                MaterialOptions.CullFaceModes.Both => CullModeFlags.FrontAndBack,
                _ => throw new ArgumentOutOfRangeException()
            },
            FrontFace = FrontFace.CounterClockwise,
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
            ColorWriteMask = ColorComponentFlags.RBit
                             | ColorComponentFlags.GBit
                             | ColorComponentFlags.BBit
                             | ColorComponentFlags.ABit,
            
            BlendEnable = true,
            
            SrcColorBlendFactor = BlendFactor.SrcAlpha,
            DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
            ColorBlendOp = BlendOp.Add,
            
            SrcAlphaBlendFactor = BlendFactor.One,
            DstAlphaBlendFactor = BlendFactor.Zero,
            AlphaBlendOp = BlendOp.Add,
        };
        
        PipelineColorBlendStateCreateInfo colorBlending = new()
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment,
        };
        
        colorBlending.BlendConstants[0] = 0;
        colorBlending.BlendConstants[1] = 0;
        colorBlending.BlendConstants[2] = 0;
        colorBlending.BlendConstants[3] = 0;

        PipelineDepthStencilStateCreateInfo depthStencil = new()
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = true,
            DepthWriteEnable = true,
            DepthCompareOp = CompareOp.LessOrEqual,
            DepthBoundsTestEnable = false,
            StencilTestEnable = false
        };

        var constantRanges = new List<PushConstantRange>();

        _vertexUniforms = new (uint off, MaterialType ty)[options.VertUniforms.Length];
        _fragmentUniforms = new (uint off, MaterialType ty)[options.FragUniforms.Length];

        uint baseOffset = 0;
        uint currentOffset = 0;
        
        for (var i = 0; i < options.VertUniforms.Length; i++)
        {
            _vertexUniforms[i] = (currentOffset, options.VertUniforms[i]);
            currentOffset += SizeOf(options.VertUniforms[i]);
        }
        if (currentOffset > 0) constantRanges.Add(new PushConstantRange
            { Offset = baseOffset, Size = currentOffset, StageFlags = ShaderStageFlags.VertexBit });

        baseOffset = currentOffset;
        currentOffset = 0;
        
        for (var i = 0; i < options.FragUniforms.Length; i++)
        {
            _fragmentUniforms[i] = (currentOffset, options.FragUniforms[i]);
            currentOffset += SizeOf(options.FragUniforms[i]);
        }
        if (currentOffset > 0) constantRanges.Add(new PushConstantRange
            { Offset = baseOffset, Size = currentOffset, StageFlags = ShaderStageFlags.FragmentBit });

        var constantRangesStack = stackalloc PushConstantRange[constantRanges.Count];
        constantRanges.CopyTo(new Span<PushConstantRange>(constantRangesStack, constantRanges.Count));
        
        _textureCount = options.TextureCount;
        var samplerBinding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = _textureCount,
            StageFlags = ShaderStageFlags.FragmentBit
        };

        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &samplerBinding
        };

        vk.CreateDescriptorSetLayout(dev, &layoutInfo, null, out var descriptorSetLayout);
        DescriptorSetLayout* setLayouts = stackalloc DescriptorSetLayout[] { descriptorSetLayout };
        
        PipelineLayoutCreateInfo pipelineLayoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            PushConstantRangeCount = (uint)constantRanges.Count,
            PPushConstantRanges = constantRangesStack,
            SetLayoutCount = 1,
            PSetLayouts = setLayouts,
        };

        if (vk.CreatePipelineLayout(dev, in pipelineLayoutInfo, null, out _pipelineLayout) != Result.Success)
            throw new Exception("failed to create pipeline layout!");
        
        var dynamicStates = stackalloc DynamicState[] { DynamicState.Viewport, DynamicState.Scissor };

        PipelineDynamicStateCreateInfo dyn = new()
        {
            SType = StructureType.PipelineDynamicStateCreateInfo,
            DynamicStateCount = 2,
            PDynamicStates = dynamicStates
        };
        
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
            PDepthStencilState = &depthStencil,
            PDynamicState = &dyn,
            Layout = _pipelineLayout,
            RenderPass = Vulkan.DefaultRenderPass,
            Subpass = 0,
            BasePipelineHandle = default
        };

        var res = vk.CreateGraphicsPipelines(dev, default, 1, in pipelineInfo,
            null, out _graphicsPipeline); 
        if (res != Result.Success) throw new Exception("failed to create graphics pipeline!");
        
        vk.DestroyShaderModule(dev, fragShaderModule, null);
        vk.DestroyShaderModule(dev, vertShaderModule, null);

        SilkMarshal.Free((nint)entryPointName);
        
        if (_textureCount == 0) return; // Create texture descriptors
        
        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1
        };

        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize,
            MaxSets = 1
        };

        vk.CreateDescriptorPool(dev, &poolInfo, null, out var descriptorPool);

        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &descriptorSetLayout
        };

        vk.AllocateDescriptorSets(dev, &allocInfo, out var descriptorSet);
        
        _descriptorPool = descriptorPool;
        _descriptorSet = descriptorSet;
    }

    
    public void UseTexture(uint index, ITexture texture)
    {
        if (index >= _textureCount) throw new IndexOutOfRangeException();
        var vkTexture = (VkTexture)texture;
        
        var descriptorImageInfo = new DescriptorImageInfo
        {
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            ImageView = vkTexture.ImageView,
            Sampler = vkTexture.ImageSampler,
        };

        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = _descriptorSet,
            DstBinding = 0,
            DstArrayElement = index,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            PImageInfo = &descriptorImageInfo
        };

        Vulkan.Vk.UpdateDescriptorSets(
            Vulkan.Device, 1, &write, 0, null);
    }
    public void BindVertexUniform(uint index, Matrix4x4 value)
    {
        if (index >= _vertexUniforms.Length) return; //throw new IndexOutOfRangeException();
        if (_vertexUniforms[index].ty != MaterialType.Mat4) throw new InvalidOperationException();
        
        Vulkan.Vk.CmdPushConstants(
            Vulkan.CurrentCommandBuffer,
            _pipelineLayout,
            ShaderStageFlags.VertexBit,
            _vertexUniforms[index].off,
            (uint)sizeof(Matrix4x4),
            ref value);
    }
    public void BindVertexUniform(uint index, int value)
    {
        if (index >= _vertexUniforms.Length) throw new IndexOutOfRangeException();
        if (_vertexUniforms[index].ty != MaterialType.Int) throw new InvalidOperationException();
        
        Vulkan.Vk.CmdPushConstants(
            Vulkan.CurrentCommandBuffer,
            _pipelineLayout,
            ShaderStageFlags.VertexBit,
            _vertexUniforms[index].off,
            sizeof(int),
            ref value);
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
    private static uint SizeOf(MaterialType type)
    {
        return type switch
        {
            MaterialType.Void => 0,
            MaterialType.Vec2 => 4 * 2,
            MaterialType.Vec3 => 4 * 3,
            MaterialType.Vec4 => 4 * 4,
            MaterialType.Float => 4,
            MaterialType.Int => 4,
            MaterialType.UInt => 4,
            MaterialType.Mat2 => 4 * 4,
            MaterialType.Mat3 => 4 * 9,
            MaterialType.Mat4 => 4 * 16,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
    
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
        var vk = Vulkan.Vk;
        var dev = Vulkan.Device;
        
        vk.DestroyDescriptorPool(dev, _descriptorPool, null);
        vk.DestroyPipeline(dev, _graphicsPipeline, null);
        vk.DestroyPipelineLayout(dev, _pipelineLayout, null);
    }
}
