using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using VoxelGame.Core;
using VoxelGame.Core.Data.Input;
using VoxelGame.Core.Math;
using IWindow = Silk.NET.Windowing.IWindow;
using SilkWin = Silk.NET.Windowing.Window;
using IWindowCore = VoxelGame.Core.IWindow;

namespace VoxelGame.Engine;

public class VkWindow : IWindowCore
{
    internal IWindow Window = null!;

    public Vec2U Position => new ((uint)Window.Position.X, (uint)Window.Position.Y);
    public Vec2U Size => new((uint)Window.FramebufferSize.X, (uint)Window.FramebufferSize.Y);
    
    public void Init()
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(800, 600),
            Title = "VoxelGame",
            ShouldSwapAutomatically = false,
        };

        Window = SilkWin.Create(options);
        
        Window.Update += Singletons.Game.Update;
        Window.Render += Singletons.Game.Draw;
        Window.Resize += (v) => ((VkGraphics)Singletons.Graphics).Resize(v.X, v.Y);

        Window.Initialize();
        ((VkInput)Singletons.Input).Initialize();
        Window.Center();
        
        if (Window.VkSurface is null) throw new Exception("Windowing platform doesn't support Vulkan.");
    }

    public void Run()
    {
        try
        {
            Singletons.Game.Init();
            while (true)
            {
                ((VkInput)Singletons.Input).Update();
                Window.DoEvents();
                if (Window.IsClosing) break;

                Window.DoRender();
                Window.DoUpdate();
            }

            Window.Reset();
        }
        catch (Exception e)
        {
            Singletons.Input.CursorMode = CursorMode.Normal;
            throw;
        }
    }
    public void Dispose() => Window.Dispose();
    
    internal unsafe SurfaceKHR VkCreateSurface(Instance instance)
        => Window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();

    internal unsafe byte** VkGetRequiredExtensions(out uint extensionCount)
        => Window.VkSurface!.GetRequiredExtensions(out extensionCount);
}
