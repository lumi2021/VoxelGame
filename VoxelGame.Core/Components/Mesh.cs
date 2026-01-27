using VoxelGame.Core.Data.Graphics;

namespace VoxelGame.Core.Components;

public class Mesh
{

    internal readonly IIndexBuffer IndexBuffer;
    internal readonly IVertexBuffer<Vec3> VertexPositionBuffer;
    internal readonly IVertexBuffer<Vec2> VertexCoordBuffer;
    
    public Vec3 Position; 
    public Vec3 Rotation; 
    public Vec3 Scale = new Vec3(1, 1, 1);

    public IMaterial Material;
    
    public Mesh()
    {
        IndexBuffer = Singletons.Graphics.GenerateIndexBuffer();
        VertexPositionBuffer = Singletons.Graphics.GenerateVertexBuffer<Vec3>();
        VertexCoordBuffer = Singletons.Graphics.GenerateVertexBuffer<Vec2>();
    }
    
    public IRenderContext? GetRenderContext()
    {
        if (Material == null!) return null!;
        var modelMatrix = Mat4.CreateFromYawPitchRoll(Deg2Rad(Rotation.X), Deg2Rad(Rotation.Y), Deg2Rad(Rotation.Z))
            * Mat4.CreateTranslation(Position)
            * Mat4.CreateScale(Scale);

        return Singletons.Graphics.Context
            .WithMaterial(Material)
            .WithMesh(IndexBuffer, [VertexPositionBuffer, VertexCoordBuffer])
            .WithVertexUniform(2, modelMatrix);
    }
}
