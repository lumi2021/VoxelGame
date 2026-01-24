namespace VoxelGame.Core.Math;

public static class Vec3Ext
{
    public static Vec3 RotateBy(this Vec3 vec, float pitch, float yal) => RotateVector(vec, pitch, yal);
    
    public static Vec3 MulScalar(this Vec3 vec, float value) => new(vec.X * value, vec.Y * value, vec.Z * value);
    public static Vec3 MulScalar(this Vec3 vec, double value) => new((float)(vec.X * value), (float)(vec.Y * value), (float)(vec.Z * value));
}
