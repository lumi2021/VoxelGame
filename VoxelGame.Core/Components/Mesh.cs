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
    
    public void Draw()
    {
        if (Material == null!) return;
        var modelMatrix = Mat4.CreateFromYawPitchRoll(Deg2Rad(Rotation.X), Deg2Rad(Rotation.Y), Deg2Rad(Rotation.Z))
            * Mat4.CreateTranslation(Position)
            * Mat4.CreateScale(Scale);
        
        Singletons.Graphics.BindMaterial(Material);
        Singletons.Graphics.BindMesh(IndexBuffer, [VertexPositionBuffer, VertexCoordBuffer]);
        Singletons.Graphics.BindMat4(2, modelMatrix);
        Singletons.Graphics.Draw();
    }
}
