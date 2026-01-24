namespace VoxelGame.Core.Math;

public readonly struct Vec2U(uint x, uint y)
{
    public readonly uint X = x;
    public readonly uint Y = y;
    
    public Vec2U() : this(0, 0) { }
}
