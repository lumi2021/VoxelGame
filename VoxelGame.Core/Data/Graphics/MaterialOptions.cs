namespace VoxelGame.Core.Data.Graphics;

public struct MaterialOptions(string vertPath, string fragPath)
{
    public readonly string VertShaderPath = vertPath;
    public readonly string FragShaderPath = fragPath;

    public MaterialType[] InstAttributes = [];
    public MaterialType[] VertAttributes = [];
    
    public MaterialType[] VertUniforms = [];
    public MaterialType[] FragUniforms = [];

    public uint TextureCount = 0;

    public CullFaceModes CullFaceMode = CullFaceModes.Back;
    public GeometryModes GeometryMode = GeometryModes.Triangles;
    
    public enum CullFaceModes
    {
        Front,
        Back,
        Both
    }
    public enum GeometryModes
    {
        Points,
        Lines,
        Triangles,
    }
}
