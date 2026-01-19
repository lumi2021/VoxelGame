namespace VoxelGame.Core.Math;

public readonly struct VecU2(uint x, uint y)
{
    public readonly uint X = x;
    public readonly uint Y = y;
    
    public VecU2() : this(0, 0) { }
}
