using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using SilkWin = Silk.NET.Windowing.Window;

namespace VoxelGame.Engine;

public static class Window
{
    private static IWindow _window = null!;
    
    public static Vector2D<uint> FramebufferSize => new ((uint)_window.FramebufferSize.X, (uint)_window.FramebufferSize.Y);
    
    public static void Init()
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "VoxelGame",
        };

        _window = SilkWin.Create(options);

        _window.Load += World.Init;
        _window.Update += World.Update;
        _window.Render += World.Draw;
        
        _window.Initialize();

        if (_window.VkSurface is null) throw new Exception("Windowing platform doesn't support Vulkan.");
    }
    public static void Run() => _window.Run();
    public static void Dispose() => _window.Dispose();
    
    internal static unsafe SurfaceKHR VkCreateSurface(Instance instance)
        => _window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
    internal static unsafe byte** VkGetRequiredExtensions(out uint extensionCount)
        => _window.VkSurface!.GetRequiredExtensions(out extensionCount);
}
