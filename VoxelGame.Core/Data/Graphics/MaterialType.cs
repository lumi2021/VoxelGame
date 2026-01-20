namespace VoxelGame.Core.Data.Graphics;

public enum MaterialType
{
    Void,
    
    Vec2,
    Vec3,
    Vec4,
    
    ColorRg32 = Vec2,
    ColorRgb32 = Vec3,
    ColorRgba32 = Vec4,
    
    Float,
    Int,
    UInt,
    
    Mat2,
    Mat3,
    Mat4,
}
