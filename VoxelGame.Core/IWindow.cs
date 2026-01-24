namespace VoxelGame.Core;

public interface IWindow
{
    
    public Vec2U Position { get; }
    public Vec2U Size { get; }
    
    public void Init();
    public void Run();
    public void Dispose();

}
