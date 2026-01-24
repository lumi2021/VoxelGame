using VoxelGame.Core.Data.Input;

namespace VoxelGame.Core;

public interface IInput
{
    public string Clipboard { get; set; }
    public string CharacterBuffer { get; }
    
    public Vec2 CursorPosition { get; }
    public Vec2 CursorOffset { get; }
    public CursorMode CursorMode { get; set; }
    
    public bool IsPressed(Key key);
    public bool IsJustPressed(Key key);
    public bool IsReleased(Key key);
    
    public bool IsPressed(MouseBtn btn);
    public bool IsJustPressed(MouseBtn btn);
    public bool IsReleased(MouseBtn btn);
}
