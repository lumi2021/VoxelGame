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

    private double _time;
    private Matrix4x4 _modelMatrix;
    
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

        ushort[] indices = [
            0, 1, 2,  0, 2, 3,
            4, 5, 6,  4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            //16, 17, 18, 16, 18, 19,
            //20, 21, 22, 20, 22, 23
        ];
        Vec3[] vertices = [
            new(0, 0, 0), new(0, 0, 1), new(1, 0, 1), new(1, 0, 0),
            new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0),
            new(0, 0, 0), new(0, 1, 0), new(0, 1, 1), new(0, 0, 1),
            new(1, 0, 0), new(1, 0, 1), new(1, 1, 1), new(1, 1, 0),
        ];
        Vec2[] uvs = [
            new(0, 0), new(1, 0), new(1, 1), new(0, 1),
            new(0, 0), new(1, 0), new(1, 1), new(0, 1),
            new(0, 0), new(1, 0), new(1, 1), new(0, 1),
            new(0, 0), new(1, 0), new(1, 1), new(0, 1),
        ];
        
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
        _time += delta;
        _modelMatrix = Matrix4x4.CreateRotationY((float)_time, new Vector3(.5f, .5f, .5f))
                       * Matrix4x4.CreateRotationX(MathF.PI / 180 * 45, new Vector3(.5f, .5f, .5f))
                       * Matrix4x4.CreateTranslation(-.5f, -.5f, 2);
        
    }

    public void Draw(double delta)
    {
        Singletons.Graphics.BeginRenderingFrame();
        
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 180 * 90, 800/600f, 0.0001f, 10f);
        var view = Matrix4x4.CreateLookTo(Vector3.Zero, Vector3.UnitZ,  Vector3.UnitY);
        
        Singletons.Graphics.BindMaterial(_material);
        Singletons.Graphics.BindMesh(_indexBuffer, [_vertexPositionBuffer, _vertexCoordBuffer]);
        Singletons.Graphics.BindMat4(0, projection);
        Singletons.Graphics.BindMat4(1, _modelMatrix);
        Singletons.Graphics.BindMat4(2, view);
        Singletons.Graphics.Draw();
        
        Singletons.Graphics.EndRenderingFrame();
    }
}
