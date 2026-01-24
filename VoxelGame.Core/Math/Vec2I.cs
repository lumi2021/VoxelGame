namespace VoxelGame.Core.Math;

public readonly struct Vec2I(int x, int y)
{
    public readonly int X = x;
    public readonly int Y = y;
    
    public Vec2I() : this(0, 0) { }
}
