using System.Runtime.CompilerServices;

namespace VoxelGame.Core.Math;

public static class Trigonometry
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Deg2Rad(float degrees) => MathF.PI / 180 * degrees;


    public static Vec3 DirectionFromRotation(Vec3 rotation)
    {
        var pitch = Deg2Rad(rotation.X);
        var yaw   = Deg2Rad(rotation.Y);
        
        var cosP = MathF.Cos(pitch);
        var sinP = MathF.Sin(pitch);
        var cosY = MathF.Cos(yaw);
        var sinY = MathF.Sin(yaw);
        
        var x = cosP * sinY;
        var y = -sinP; 
        var z = cosP * cosY;

        return new Vec3(x, y, z);
    }

    public static Vec3 RotateVector(Vec3 vector, float pitch, float yaw)
    {
        var pRad = Deg2Rad(pitch);
        var yRad = Deg2Rad(yaw);
        
        var cosP = MathF.Cos(pRad);
        var sinP = MathF.Sin(pRad);
        var cosY = MathF.Cos(yRad);
        var sinY = MathF.Sin(yRad);
        
        var x = vector.X * cosY + (vector.Y * sinP * sinY + vector.Z * cosP * sinY);
        var y = vector.Y * cosP - vector.Z * sinP;
        var z = -vector.X * sinY + (vector.Y * sinP * cosY + vector.Z * cosP * cosY);

        return new Vec3(x, y, z);
    }
}
