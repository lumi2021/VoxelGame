using Silk.NET.Vulkan;
using VoxelGame.Engine.GraphicsImpl;

namespace VoxelGame.Engine;

public static class Graphics
{
    internal static Vk vk = null!;

    public static void Init() => Vulkan.Init();
    public static void CleanUp() => Vulkan.CleanUp();
    public static void BeginRenderingFrame() => Vulkan.BeginRenderingFrame();
    public static void MidRenderFrame() => Vulkan.MidRenderingFrame();
    public static void EndRenderingFrame() => Vulkan.EndRenderingFrame();
}
