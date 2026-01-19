namespace VoxelGame.Core.Math;

public readonly struct VecI2(int x, int y)
{
    public readonly int X = x;
    public readonly int Y = y;
    
    public VecI2() : this(0, 0) { }
}
