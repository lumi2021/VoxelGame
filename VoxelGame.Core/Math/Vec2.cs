namespace VoxelGame.Core.Math;

public readonly struct Vec2(float x, float y)
{
    public readonly float X = x;
    public readonly float Y = y;
    
    public Vec2() : this(0, 0) { }
}
