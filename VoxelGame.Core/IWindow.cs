using VoxelGame.Core.Math;

namespace VoxelGame.Core;

public interface IWindow
{
    
    public VecU2 Position { get; }
    public VecU2 Size { get; }
    
    public void Init();
    public void Run();
    public void Dispose();

}
