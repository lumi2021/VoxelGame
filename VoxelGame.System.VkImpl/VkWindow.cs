using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using VoxelGame.Core;
using VoxelGame.Core.Math;
using IWindow = Silk.NET.Windowing.IWindow;
using SilkWin = Silk.NET.Windowing.Window;
using IWindowCore = VoxelGame.Core.IWindow;

namespace VoxelGame.Engine;

public class VkWindow : IWindowCore
{
    private static IWindow _window = null!;

    public VecU2 Position => new ((uint)_window.Position.X, (uint)_window.Position.Y);
    public VecU2 Size => new((uint)_window.FramebufferSize.X, (uint)_window.FramebufferSize.Y);
    
    public void Init()
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "VoxelGame",
        };

        _window = SilkWin.Create(options);

        _window.Load += Singletons.Game.Init;
        _window.Update += Singletons.Game.Update;
        _window.Render += Singletons.Game.Draw;

        _window.Initialize();
      
        if (_window.VkSurface is null) throw new Exception("Windowing platform doesn't support Vulkan.");
    }
    
    public void Run() => _window.Run();
    public void Dispose() => _window.Dispose();
    
    internal static unsafe SurfaceKHR VkCreateSurface(Instance instance)
        => _window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();

    internal static unsafe byte** VkGetRequiredExtensions(out uint extensionCount)
        => _window.VkSurface!.GetRequiredExtensions(out extensionCount);
}
