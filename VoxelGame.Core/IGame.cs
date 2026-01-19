namespace VoxelGame.Core;

public interface IGame
{
    public void Init();
    public void Deinit();
    public void Update(double delta);
    public void Draw(double delta);
}
