using System.Diagnostics;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using VoxelGame.Core.Data.Graphics;

namespace VoxelGame.Engine.GraphicsImpl;

public unsafe class VkMaterial : IMaterial, IDisposable
{
    
    private Pipeline _graphicsPipeline;
    private PipelineLayout _pipelineLayout;
    public Pipeline GraphicsPipeline => _graphicsPipeline;
    public PipelineLayout PipelineLayout => _pipelineLayout;
    
    internal VkMaterial(
        string vertPath, string fragPath,
        MaterialType[] attrType,
        MaterialType[] vertexUniforms,
        MaterialType[] fragmentUniforms)
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
            if (attrType[i] == MaterialType.Void) continue;
            
            var format = attrType[i] switch
            {
                MaterialType.Vec2 => Format.R32G32Sfloat,
                MaterialType.Vec3 => Format.R32G32B32Sfloat,
                MaterialType.Vec4 => Format.R32G32B32A32Sfloat,
                MaterialType.Float => Format.R32Sfloat,
                MaterialType.Int => Format.R32Sint,
                MaterialType.UInt => Format.R32Uint,
                _ => throw new UnreachableException()
            };
            var stride = (uint)SizeOf(attrType[i]);
            
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
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.BackBit,
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

        PipelineDepthStencilStateCreateInfo depthStencil = new()
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = true,
            DepthWriteEnable = true,
            DepthCompareOp = CompareOp.LessOrEqual,
            DepthBoundsTestEnable = false,
            StencilTestEnable = false
        };

        var constantRanges = stackalloc[]
        {
            new PushConstantRange { Offset = 0, Size = 0, StageFlags = ShaderStageFlags.VertexBit },
            new PushConstantRange { Offset = 0, Size = 0, StageFlags = ShaderStageFlags.FragmentBit },
        };
        
        var fullSize = vertexUniforms.Aggregate<MaterialType, uint>(0, (current, i) => current + (uint)SizeOf(i));
        constantRanges[0].Size = fullSize;
        constantRanges[1].Offset = fullSize;
        fullSize = fragmentUniforms.Aggregate<MaterialType, uint>(0, (current, i) => current + (uint)SizeOf(i));
        constantRanges[1].Size = Math.Max(4, fullSize);
        
        PipelineLayoutCreateInfo pipelineLayoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 0,
            PushConstantRangeCount = 2,
            PPushConstantRanges = constantRanges,
        };

        if (vk.CreatePipelineLayout(dev, in pipelineLayoutInfo, null, out _pipelineLayout) != Result.Success)
            throw new Exception("failed to create pipeline layout!");
        
        var dynamicStates = stackalloc DynamicState[]
        {
            DynamicState.Viewport,
            DynamicState.Scissor
        };

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

    private static int SizeOf(MaterialType type)
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
        
        vk.DestroyPipeline(dev, _graphicsPipeline, null);
        vk.DestroyPipelineLayout(dev, _pipelineLayout, null);
    }
}
