using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VoxelGame.Engine.GraphicsImpl;

internal static unsafe class Vulkan
{
#if DEBUG
    private const bool EnableValidationLayers = true;
#else
    private const bool EnableValidationLayers = false;
#endif
    
    private const int MaxFramesInFlight = 2;

    private static Vk Vk => Graphics.vk;
    private static Instance _instance;

    private static ExtDebugUtils? _debugUtils;
    private static DebugUtilsMessengerEXT _debugMessenger;
    private static KhrSurface? _khrSurface;
    private static SurfaceKHR _surface;

    private static PhysicalDevice _physicalDevice;
    private static Device _device;

    private static Queue _graphicsQueue;
    private static Queue _presentQueue;

    private static KhrSwapchain? _khrSwapChain;
    private static SwapchainKHR _swapChain;
    private static Image[]? _swapChainImages;
    private static Format _swapChainImageFormat;
    private static Extent2D _swapChainExtent;
    private static ImageView[]? _swapChainImageViews;
    private static Framebuffer[]? _swapChainFramebuffers;

    private static RenderPass _renderPass;
    private static PipelineLayout _pipelineLayout;
    private static Pipeline _graphicsPipeline;

    private static CommandPool _commandPool;
    private static CommandBuffer[]? _commandBuffers;

    private static Semaphore[]? _imageAvailableSemaphores;
    private static Semaphore[]? _renderFinishedSemaphores;
    private static Fence[]? _inFlightFences;
    private static Fence[]? _imagesInFlight;
    
    private static int _currentFrame = 0;
    private static uint _imageIndex = 0;
    private static CommandBuffer CurrentCommandBuffer => _commandBuffers![_currentFrame];
    private static Framebuffer CurrentFramebuffer => _swapChainFramebuffers![_imageIndex];
    
    private static readonly string[] ValidationLayers = ["VK_LAYER_KHRONOS_validation"];
    private static readonly string[] DeviceExtensions = [ KhrSwapchain.ExtensionName ];
    
    internal static void Init()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapChain();
        CreateImageViews();
        CreateRenderPass();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        CreateCommandPool();
        CreateCommandBuffers();
        CreateSyncObjects();
    }
    internal static void CleanUp()
    {
        for (var i = 0; i < MaxFramesInFlight; i++)
        {
            Vk!.DestroySemaphore(_device, _renderFinishedSemaphores![i], null);
            Vk!.DestroySemaphore(_device, _imageAvailableSemaphores![i], null);
            Vk!.DestroyFence(_device, _inFlightFences![i], null);
        }

        Vk!.DestroyCommandPool(_device, _commandPool, null);

        foreach (var framebuffer in _swapChainFramebuffers!)
            Vk!.DestroyFramebuffer(_device, framebuffer, null);

        Vk!.DestroyPipeline(_device, _graphicsPipeline, null);
        Vk!.DestroyPipelineLayout(_device, _pipelineLayout, null);
        Vk!.DestroyRenderPass(_device, _renderPass, null);

        foreach (var imageView in _swapChainImageViews!)
            Vk!.DestroyImageView(_device, imageView, null);

        _khrSwapChain!.DestroySwapchain(_device, _swapChain, null);

        Vk!.DestroyDevice(_device, null);

        if (EnableValidationLayers) _debugUtils!.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);

        _khrSurface!.DestroySurface(_instance, _surface, null);
        Vk!.DestroyInstance(_instance, null);
        Vk!.Dispose();

        Window.Dispose();
    }

    internal static void BeginRenderingFrame()
    {
        Vk.WaitForFences(_device, 1, in _inFlightFences![_currentFrame], true, ulong.MaxValue);

        _imageIndex = 0;
        _khrSwapChain!.AcquireNextImage(_device, _swapChain, ulong.MaxValue,
            _imageAvailableSemaphores![_currentFrame], default, ref _imageIndex);

        if (_imagesInFlight![_imageIndex].Handle != 0)
            Vk.WaitForFences(_device, 1, in _imagesInFlight[_imageIndex], true, ulong.MaxValue);
        _imagesInFlight[_imageIndex] = _inFlightFences[_currentFrame];
        
        CommandBufferBeginInfo beginInfo = new() { SType = StructureType.CommandBufferBeginInfo };

        if (Vk!.BeginCommandBuffer(_commandBuffers![_currentFrame], in beginInfo) != Result.Success)
            throw new Exception("failed to begin recording command buffer!");
    }

    internal static void MidRenderingFrame()
    {
        ClearValue clearColor = new()
            { Color = new ClearColorValue { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 } };
        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _renderPass,
            Framebuffer = CurrentFramebuffer,
            RenderArea =
            {
                Offset = { X = 0, Y = 0 },
                Extent = _swapChainExtent,
            },
            ClearValueCount = 1,
            PClearValues = &clearColor,
        };
        Vk!.CmdBeginRenderPass(CurrentCommandBuffer, &renderPassInfo, SubpassContents.Inline);

        Vk!.CmdBindPipeline(CurrentCommandBuffer, PipelineBindPoint.Graphics, _graphicsPipeline);
        Vk!.CmdDraw(CurrentCommandBuffer, 3, 1, 0, 0);

        Vk!.CmdEndRenderPass(CurrentCommandBuffer);
    }
    
    internal static void EndRenderingFrame()
    {
        if (Vk!.EndCommandBuffer(_commandBuffers![_currentFrame]) != Result.Success)
            throw new Exception("failed to record command buffer!");

        var waitSemaphores = stackalloc[] { _imageAvailableSemaphores![_currentFrame] };
        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

        var buffer = _commandBuffers![_currentFrame];
        SubmitInfo submitInfo = new() {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,

            CommandBufferCount = 1,
            PCommandBuffers = &buffer
        };

        var signalSemaphores = stackalloc[] { _renderFinishedSemaphores![_currentFrame] };
        submitInfo = submitInfo with
        {
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };

        Vk.ResetFences(_device, 1, in _inFlightFences![_currentFrame]);

        if (Vk.QueueSubmit(_graphicsQueue, 1, in submitInfo, _inFlightFences![_currentFrame]) != Result.Success)
            throw new Exception("failed to submit draw command buffer!");

        var swapChains = stackalloc[] { _swapChain };
        fixed (uint* imgidx = &_imageIndex)
        {
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,

                WaitSemaphoreCount = 1,
                PWaitSemaphores = signalSemaphores,

                SwapchainCount = 1,
                PSwapchains = swapChains,

                PImageIndices = imgidx
            };
            _khrSwapChain!.QueuePresent(_presentQueue, in presentInfo);
        }
        _currentFrame = (_currentFrame + 1) % MaxFramesInFlight;
    }
    
     private static void CreateInstance()
    {
        Graphics.vk = Vk.GetApi();
        
        if (EnableValidationLayers && !CheckValidationLayerSupport())
            throw new Exception("validation layers requested, but not available!");

        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version12
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var extensions = GetRequiredExtensions();
        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions); ;

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)ValidationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(ValidationLayers);

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (Vk.CreateInstance(in createInfo, null, out _instance) != Result.Success)
        {
            throw new Exception("failed to create instance!");
        }

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }

    private static void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
    }

    private static void SetupDebugMessenger()
    {
        if (!EnableValidationLayers) return;
        
        if (!Vk!.TryGetInstanceExtension(_instance, out _debugUtils)) return;

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (_debugUtils!.CreateDebugUtilsMessenger(_instance, in createInfo, null, out _debugMessenger) != Result.Success)
            throw new Exception("failed to set up debug messenger!");
    }

    private static void CreateSurface()
    {
        if (!Vk!.TryGetInstanceExtension<KhrSurface>(_instance, out _khrSurface))
            throw new NotSupportedException("KHR_surface extension not found.");
        _surface = Window.VkCreateSurface(_instance);
    }

    private static void PickPhysicalDevice()
    {
        var devices = Vk!.GetPhysicalDevices(_instance);

        foreach (var dev in devices)
        {
            if (!IsDeviceSuitable(dev)) continue;
            _physicalDevice = dev;

            Vk.GetPhysicalDeviceProperties(_physicalDevice, out var properties);
            Console.WriteLine($"Device name: {SilkMarshal.PtrToString((nint)properties.DeviceName)}");
            Console.WriteLine($"API version: {properties.ApiVersion}");
            break;
        }

        if (_physicalDevice.Handle == 0) throw new Exception("failed to find a suitable GPU!");
    }

    private static void CreateLogicalDevice()
    {
        var indices = FindQueueFamilies(_physicalDevice);

        var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
        var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        var queuePriority = 1.0f;
        for (var i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }

        PhysicalDeviceFeatures deviceFeatures = new();

        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,

            PEnabledFeatures = &deviceFeatures,

            EnabledExtensionCount = (uint)DeviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(DeviceExtensions)
        };

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)ValidationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(ValidationLayers);
        }
        else createInfo.EnabledLayerCount = 0;

        if (Vk!.CreateDevice(_physicalDevice, in createInfo, null, out _device) != Result.Success)
            throw new Exception("failed to create logical device!");

        Vk!.GetDeviceQueue(_device, indices.GraphicsFamily!.Value, 0, out _graphicsQueue);
        Vk!.GetDeviceQueue(_device, indices.PresentFamily!.Value, 0, out _presentQueue);

        if (EnableValidationLayers) SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

    }

    private static void CreateSwapChain()
    {
        var swapChainSupport = QuerySwapChainSupport(_physicalDevice);

        var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
        var extent = ChooseSwapExtent(swapChainSupport.Capabilities);

        var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
        if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            imageCount = swapChainSupport.Capabilities.MaxImageCount;

        SwapchainCreateInfoKHR creatInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _surface,

            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            PresentMode = PresentModeKHR.FifoKhr
        };

        var indices = FindQueueFamilies(_physicalDevice);
        var queueFamilyIndices = stackalloc[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

        if (indices.GraphicsFamily != indices.PresentFamily)
        {
            creatInfo = creatInfo with
            {
                ImageSharingMode = SharingMode.Concurrent,
                QueueFamilyIndexCount = 2,
                PQueueFamilyIndices = queueFamilyIndices,
            };
        }
        else creatInfo.ImageSharingMode = SharingMode.Exclusive;

        creatInfo = creatInfo with
        {
            PreTransform = swapChainSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,

            OldSwapchain = default
        };

        if (!Vk!.TryGetDeviceExtension(_instance, _device, out _khrSwapChain))
            throw new NotSupportedException("VK_KHR_swapchain extension not found.");

        if (_khrSwapChain!.CreateSwapchain(_device, in creatInfo, null, out _swapChain) != Result.Success)
            throw new Exception("failed to create swap chain!");

        _khrSwapChain.GetSwapchainImages(_device, _swapChain, ref imageCount, null);
        _swapChainImages = new Image[imageCount];
        fixed (Image* swapChainImagesPtr = _swapChainImages)
        {
            _khrSwapChain.GetSwapchainImages(_device, _swapChain, ref imageCount, swapChainImagesPtr);
        }

        _swapChainImageFormat = surfaceFormat.Format;
        _swapChainExtent = extent;
    }

    private static void CreateImageViews()
    {
        _swapChainImageViews = new ImageView[_swapChainImages!.Length];

        for (var i = 0; i < _swapChainImages.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = _swapChainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = _swapChainImageFormat,
                Components =
                {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity,
                },
                SubresourceRange =
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                }

            };

            if (Vk!.CreateImageView(_device, in createInfo, null, out _swapChainImageViews[i]) != Result.Success)
                throw new Exception("failed to create image views!");
        }
    }

    private static void CreateRenderPass()
    {
        AttachmentDescription colorAttachment = new()
        {
            Format = _swapChainImageFormat,
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

        if (Vk!.CreateRenderPass(_device, in renderPassInfo, null, out _renderPass) != Result.Success)
        {
            throw new Exception("failed to create render pass!");
        }
    }

    private static void CreateGraphicsPipeline()
    {
        var vertShaderCode = File.ReadAllBytes("shaders/base.vert.spv");
        var fragShaderCode = File.ReadAllBytes("shaders/base.frag.spv");

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

        PipelineVertexInputStateCreateInfo vertexInputInfo = new()
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 0,
            VertexAttributeDescriptionCount = 0,
        };

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
            Width = _swapChainExtent.Width,
            Height = _swapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };

        Rect2D scissor = new()
        {
            Offset = { X = 0, Y = 0 },
            Extent = _swapChainExtent,
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

        if (Vk!.CreatePipelineLayout(_device, in pipelineLayoutInfo, null, out _pipelineLayout) != Result.Success)
            throw new Exception("failed to create pipeline layout!");

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

        if (Vk!.CreateGraphicsPipelines(_device, default, 1, in pipelineInfo,
                null, out _graphicsPipeline) != Result.Success)
        {
            throw new Exception("failed to create graphics pipeline!");
        }
        
        Vk!.DestroyShaderModule(_device, fragShaderModule, null);
        Vk!.DestroyShaderModule(_device, vertShaderModule, null);

        SilkMarshal.Free((nint)vertShaderStageInfo.PName);
        SilkMarshal.Free((nint)fragShaderStageInfo.PName);
    }

    private static void CreateFramebuffers()
    {
        _swapChainFramebuffers = new Framebuffer[_swapChainImageViews!.Length];

        for (var i = 0; i < _swapChainImageViews.Length; i++)
        {
            var attachment = _swapChainImageViews[i];

            FramebufferCreateInfo framebufferInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = _renderPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = _swapChainExtent.Width,
                Height = _swapChainExtent.Height,
                Layers = 1,
            };

            if (Vk!.CreateFramebuffer(_device, in framebufferInfo, null, out _swapChainFramebuffers[i]) != Result.Success)
                throw new Exception("failed to create framebuffer!");
        }
    }

    private static void CreateCommandPool()
    {
        var queueFamilyIndices = FindQueueFamilies(_physicalDevice);

        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
        };

        if (Vk!.CreateCommandPool(_device, in poolInfo, null, out _commandPool) != Result.Success)
            throw new Exception("failed to create command pool!");
    }

    private static void CreateCommandBuffers()
    {
        _commandBuffers = new CommandBuffer[_swapChainFramebuffers!.Length];

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)_commandBuffers.Length,
        };

        fixed (CommandBuffer* commandBuffersPtr = _commandBuffers)
        {
            if (Vk!.AllocateCommandBuffers(_device, in allocInfo, commandBuffersPtr) != Result.Success)
                throw new Exception("failed to allocate command buffers!");
        }
    }

    private static void CreateSyncObjects()
    {
        _imageAvailableSemaphores = new Semaphore[MaxFramesInFlight];
        _renderFinishedSemaphores = new Semaphore[MaxFramesInFlight];
        _inFlightFences = new Fence[MaxFramesInFlight];
        _imagesInFlight = new Fence[_swapChainImages!.Length];

        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo,
        };

        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };

        for (var i = 0; i < MaxFramesInFlight; i++)
        {
            if (Vk!.CreateSemaphore(_device, in semaphoreInfo, null, out _imageAvailableSemaphores[i]) != Result.Success ||
                Vk!.CreateSemaphore(_device, in semaphoreInfo, null, out _renderFinishedSemaphores[i]) != Result.Success ||
                Vk!.CreateFence(_device, in fenceInfo, null, out _inFlightFences[i]) != Result.Success)
            {
                throw new Exception("failed to create synchronization objects for a frame!");
            }
        }
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

            if (Vk!.CreateShaderModule(_device, in createInfo, null, out shaderModule) != Result.Success)
            {
                throw new Exception();
            }
        }

        return shaderModule;

    }

    private static SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
    {
        foreach (var availableFormat in availableFormats)
        {
            if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    private static PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
    {
        foreach (var availablePresentMode in availablePresentModes)
        {
            if (availablePresentMode == PresentModeKHR.MailboxKhr)
            {
                return availablePresentMode;
            }
        }

        return PresentModeKHR.FifoKhr;
    }

    private static Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue) return capabilities.CurrentExtent;
        
        var framebufferSize = Window.FramebufferSize;

        Extent2D actualExtent = new()
        {
            Width = framebufferSize.X,
            Height = framebufferSize.Y
        };

        actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
        actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

        return actualExtent;
    }

    private static SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice)
    {
        var details = new SwapChainSupportDetails();

        _khrSurface!.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, _surface, out details.Capabilities);

        uint formatCount = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, _surface, ref formatCount, null);

        if (formatCount != 0)
        {
            details.Formats = new SurfaceFormatKHR[formatCount];
            fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
            {
                _khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, _surface, ref formatCount, formatsPtr);
            }
        }
        else
        {
            details.Formats = [];
        }

        uint presentModeCount = 0;
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, _surface, ref presentModeCount, null);

        if (presentModeCount != 0)
        {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            fixed (PresentModeKHR* formatsPtr = details.PresentModes)
            {
                _khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, _surface, ref presentModeCount, formatsPtr);
            }

        }
        else
        {
            details.PresentModes = Array.Empty<PresentModeKHR>();
        }

        return details;
    }

    private static bool IsDeviceSuitable(PhysicalDevice device)
    {
        var indices = FindQueueFamilies(device);

        bool extensionsSupported = CheckDeviceExtensionsSupport(device);

        bool swapChainAdequate = false;
        if (extensionsSupported)
        {
            var swapChainSupport = QuerySwapChainSupport(device);
            swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
        }

        return indices.IsComplete() && extensionsSupported && swapChainAdequate;
    }

    private static bool CheckDeviceExtensionsSupport(PhysicalDevice device)
    {
        uint extentionsCount = 0;
        Vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, null);

        var availableExtensions = new ExtensionProperties[extentionsCount];
        fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
        {
            Vk!.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extentionsCount, availableExtensionsPtr);
        }

        var availableExtensionNames = availableExtensions.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();

        return DeviceExtensions.All(availableExtensionNames.Contains);

    }

    private static QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
    {
        var indices = new QueueFamilyIndices();

        uint queueFamilityCount = 0;
        Vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            Vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
        }


        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            _khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, _surface, out var presentSupport);

            if (presentSupport)
            {
                indices.PresentFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }

    private static string[] GetRequiredExtensions()
    {
        var glfwExtensions =Window.VkGetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);

        return EnableValidationLayers ? extensions.Append(ExtDebugUtils.ExtensionName).ToArray() : extensions;
    }

    private static bool CheckValidationLayerSupport()
    {
        uint layerCount = 0;
        Vk!.EnumerateInstanceLayerProperties(ref layerCount, null);
        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            Vk!.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

        return ValidationLayers.All(availableLayerNames.Contains);
    }

    private static void WaitDeviceIdle() => Vk.DeviceWaitIdle(_device);
    
    private static uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        Console.WriteLine($"# vk validation layer: " + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));
        return Vk.False;
    }


    private struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete() => GraphicsFamily.HasValue && PresentFamily.HasValue;
    }
    private struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }
}
