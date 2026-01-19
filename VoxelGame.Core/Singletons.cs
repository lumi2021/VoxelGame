namespace VoxelGame.Core;

public static class Singletons
{
    public static IGame Game { get; private set; }
    public static IGraphics Graphics { get; private set; }
    public static IWindow Window { get; private set; }
    private static bool _isReady = false;
    

    public static void Init(IGame game, IGraphics graphics, IWindow win)
    {
        if (_isReady) throw new InvalidOperationException("Already initialized");
        Game = game;
        Graphics = graphics;
        Window = win;
        _isReady = true;
    }
}