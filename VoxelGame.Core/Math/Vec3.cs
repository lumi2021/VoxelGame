namespace VoxelGame.Core.Math;

public readonly struct Vec3(float x, float y, float z)
{
    public readonly float X = x;
    public readonly float Y = y;
    public readonly float Z = z;
    
    public Vec3() : this(0, 0, 0) { }

    public override string ToString() => $"({X}, {Y}, {Z})";
}
