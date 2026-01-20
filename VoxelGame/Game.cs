using System.Numerics;
using VoxelGame.Core;
using VoxelGame.Core.Data.Graphics;
using VoxelGame.Core.Math;
using VoxelGame.Core.World;

namespace VoxelGame;

public class Game : IGame
{

    private Chunk _chunk;
    private IMaterial _material;
    
    private IIndexBuffer _indexBuffer;
    private IVertexBuffer<Vec3> _vertexPositionBuffer;
    private IVertexBuffer<Vec2> _vertexCoordBuffer;
    
    public void Init()
    {
        Console.WriteLine("Initializing rendering server...");
        Singletons.Graphics.Init();

        _chunk = new Chunk(64, 64, 64);
        
        _material = Singletons.Graphics.GenerateMaterial(
            "shaders/chunk.vert.spv",
            "shaders/chunk.frag.spv",
            [MaterialType.Vec3, MaterialType.Vec2],
            [MaterialType.Mat4, MaterialType.Mat4, MaterialType.Mat4],
            []
            );

        _indexBuffer = Singletons.Graphics.GenerateIndexBuffer();
        _vertexPositionBuffer = Singletons.Graphics.GenerateVertexBuffer<Vec3>();
        _vertexCoordBuffer = Singletons.Graphics.GenerateVertexBuffer<Vec2>();

        ushort[] indices = [ 0, 1, 2, 0, 2, 3 ];
        Vec3[] vertices = [ new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0) ];
        Vec2[] uvs = [ new(0, 0), new(1, 0), new(1, 1), new(0, 1) ];
        
        _indexBuffer.Fetch(indices);
        _vertexPositionBuffer.Fetch(vertices);
        _vertexCoordBuffer.Fetch(uvs);
    }

    public void Deinit()
    {
        Singletons.Graphics.CleanUp();
    }
    
    public void Update(double delta)
    {
        
    }

    public void Draw(double delta)
    {
        Singletons.Graphics.BeginRenderingFrame();
        
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 180 * 90f, 800/600f, 0.0001f, 100000f);
        var view = Matrix4x4.CreateLookTo(Vector3.Zero, Vector3.UnitZ,  Vector3.UnitY);
        var model = Matrix4x4.CreateTranslation(0, 0, 5);
        
        Singletons.Graphics.BindMaterial(_material);
        Singletons.Graphics.BindMesh(_indexBuffer, [_vertexPositionBuffer, _vertexCoordBuffer]);
        Singletons.Graphics.BindMat4(0, projection);
        Singletons.Graphics.BindMat4(1, model);
        Singletons.Graphics.BindMat4(2, view);
        Singletons.Graphics.Draw();
        
        Singletons.Graphics.EndRenderingFrame();
    }
}
