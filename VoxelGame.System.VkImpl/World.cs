namespace VoxelGame.Engine;

public static class World
{

    public static void Init()
    {
        Graphics.Init();
    }

    public static void Deinit()
    {
        Graphics.CleanUp();
    }
    
    public static void Update(double delta)
    {
        Console.WriteLine($"Fps: {1f / delta}");
    }

    public static void Draw(double delta)
    {
        Graphics.BeginRenderingFrame();
        Graphics.MidRenderFrame();
        Graphics.EndRenderingFrame();
    }
}
