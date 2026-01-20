namespace VoxelGame.Core;

public static class Singletons
{
    public static IGame Game { get; private set; }
    public static IGraphics Graphics { get; private set; }
    public static IWindow Window { get; private set; }
    public static IInput Input { get; private set; }
    
    private static bool _isReady;
    

    public static void Init(IGame game, IGraphics graphics, IWindow win, IInput input)
    {
        if (_isReady) throw new InvalidOperationException("Already initialized");
        
        Game = game;
        Graphics = graphics;
        Window = win;
        Input = input;
        
        _isReady = true;
    }
}