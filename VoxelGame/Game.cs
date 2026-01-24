using System.Numerics;
using VoxelGame.Core;
using VoxelGame.Core.Components;
using VoxelGame.Core.Data.Graphics;
using VoxelGame.Core.Data.Input;
using VoxelGame.Core.World;

namespace VoxelGame;

public class Game : IGame
{
    
    private FreeCamera _camera;
    private Chunk _chunk;
    private Mesh _mesh;
    private ITexture _texture;
    private Block[] _blocks = [
        default, // air
        new() { Tc = (0, 0), Bc = (0, 2), Lc = (0, 1), Rc = (0, 1), Nc = (0, 1), Sc = (0, 1), Name = "Grass" },
        new() { Tc = (0, 2), Bc = (0, 2), Lc = (0, 2), Rc = (0, 2), Nc = (0, 2), Sc = (0, 2), Name = "Dirt" },
        new() { Tc = (2, 2), Bc = (2, 2), Lc = (2, 2), Rc = (2, 2), Nc = (2, 2), Sc = (2, 2), Name = "Stone" },
        new() { Tc = (3, 0), Bc = (3, 0), Lc = (3, 0), Rc = (3, 0), Nc = (3, 0), Sc = (3, 0), Name = "Debug" },
    ];
    
    private double _time;
    
    private struct Block
    {
        public string Name;
        public (byte x, byte y) Tc;
        public (byte x, byte y) Bc;
        public (byte x, byte y) Lc;
        public (byte x, byte y) Rc;
        public (byte x, byte y) Nc;
        public (byte x, byte y) Sc;
    }
    
    public void Init()
    {
        Console.WriteLine("Initializing rendering server...");
        Singletons.Graphics.Init();

        _chunk = new Chunk(64, 64, 64);
        _camera = new FreeCamera
        {
            Position = new Vector3(0, 3, -3),
            Rotation = new Vector3(0, 0, 0),
        };
        _mesh = new Mesh
        {
            Material = Singletons.Graphics.GenerateMaterial(
                "shaders/chunk.vert.spv",
                "shaders/chunk.frag.spv",
                [MaterialType.Vec3, MaterialType.Vec2],
                [MaterialType.Mat4, MaterialType.Mat4, MaterialType.Mat4],
                [],
                1
            )
        };
        _texture = Singletons.Graphics.GenerateTexture("assets/textures/blocksatlas.png");
        _mesh.Material.UseTexture(0, _texture);
        
        var mb = new MeshBuilder();

        for (var z = 0; z < _chunk.SizeZ; z++)
        {
            for (var y = 0; y < _chunk.SizeY; y++)
            {
                for (var x = 0; x < _chunk.SizeX; x++)
                {
                    _chunk.Data[x, y, z]= y switch
                    {
                        60 => 1,
                        < 60 and > 50 => 2,
                        < 60 => 3,
                        _ => 0
                    };
                }
            }
        }

        for (var z = 0; z < _chunk.SizeZ; z++)
        {
            for (var y = 0; y < _chunk.SizeY; y++)
            {
                for (var x = 0; x < _chunk.SizeX; x++)
                {
                    if (_chunk.Data[x, y, z] == 0) continue;
                    var block = _blocks[_chunk.Data[x, y, z]];

                    if (!_chunk.Data.TryGetValue(x, y, z+1, out var v) || v == 0)
                        mb.AddQuad( // Front
                            new Vector3(x+1, y, z+1), new Vector3(x+1, y+1, z+1), new Vector3(x, y+1, z+1), new Vector3(x, y, z+1),
                            new Vector2(block.Nc.x + 0, block.Nc.y + 1), // BL
                            new Vector2(block.Nc.x + 0, block.Nc.y + 0), // TL
                            new Vector2(block.Nc.x + 1, block.Nc.y + 0), // TR
                            new Vector2(block.Nc.x + 1, block.Nc.y + 1)  // BR
                            );
                    
                    if (!_chunk.Data.TryGetValue(x, y, z-1, out v) || v == 0)
                        mb.AddQuad( // Back
                            new Vector3(x, y, z), new Vector3(x, y+1, z), new Vector3(x+1, y+1, z), new Vector3(x+1, y, z),
                            new Vector2(block.Sc.x + 0, block.Sc.y + 1), // BL
                            new Vector2(block.Sc.x + 0, block.Sc.y + 0), // TL
                            new Vector2(block.Sc.x + 1, block.Sc.y + 0), // TR
                            new Vector2(block.Sc.x + 1, block.Sc.y + 1)  // BR
                            );
                    
                    if (!_chunk.Data.TryGetValue(x-1, y, z, out v) || v == 0)
                        mb.AddQuad( // Left
                            new Vector3(x, y, z), new Vector3(x, y, z+1), new Vector3(x, y+1, z+1), new Vector3(x, y+1, z),
                            new Vector2(block.Lc.x + 1, block.Lc.y + 1), // BR
                            new Vector2(block.Lc.x + 0, block.Lc.y + 1), // BL
                            new Vector2(block.Lc.x + 0, block.Lc.y + 0), // TL
                            new Vector2(block.Lc.x + 1, block.Lc.y + 0)  // TR
                            );
                    
                    if (!_chunk.Data.TryGetValue(x+1, y, z, out v) || v == 0)
                        mb.AddQuad( // Right
                            new Vector3(x+1, y, z), new Vector3(x+1, y+1, z), new Vector3(x+1, y+1, z+1), new Vector3(x+1, y, z+1),
                            new Vector2(block.Rc.x + 0, block.Rc.y + 1), // BL
                            new Vector2(block.Rc.x + 0, block.Rc.y + 0), // TL
                            new Vector2(block.Rc.x + 1, block.Rc.y + 0), // TR
                            new Vector2(block.Rc.x + 1, block.Rc.y + 1)  // BR
                            );
                    
                    if (!_chunk.Data.TryGetValue(x, y+1, z, out v) || v == 0)
                        mb.AddQuad( // Top
                            new Vector3(x, y+1, z), new Vector3(x, y+1, z+1), new Vector3(x+1, y+1, z+1), new Vector3(x+1, y+1, z),
                            new Vector2(block.Tc.x + 0, block.Tc.y + 1), // BL
                            new Vector2(block.Tc.x + 0, block.Tc.y + 0), // TL
                            new Vector2(block.Tc.x + 1, block.Tc.y + 0), // TR
                            new Vector2(block.Tc.x + 1, block.Tc.y + 1)  // BR
                            );
                    
                    if (!_chunk.Data.TryGetValue(x, y-1, z, out v) || v == 0)
                        mb.AddQuad( // Bottom
                            new Vector3(x, y, z), new Vector3(x+1, y, z), new Vector3(x+1, y, z+1), new Vector3(x, y, z+1),
                            new Vector2(block.Bc.x + 0, block.Bc.y + 0), // TL
                            new Vector2(block.Bc.x + 1, block.Bc.y + 0), // TR
                            new Vector2(block.Bc.x + 1, block.Bc.y + 1), // BR
                            new Vector2(block.Bc.x + 0, block.Bc.y + 1)  // BL
                            );
                }
            }
        }
        
        Console.WriteLine($"Committing mesh with {mb.TriangleCount} triangles");
        mb.Commit(_mesh);

        Singletons.Input.CursorMode = CursorMode.Captured;
    }

    public void Deinit()
    {
        Singletons.Graphics.CleanUp();
    }
    
    public void Update(double delta)
    {
        _time += delta;
        _camera.Update(delta);
    }

    public void Draw(double delta)
    {
        Singletons.Graphics.BeginRenderingFrame();
        
        Singletons.Graphics.BindMat4(0, _camera.Projection * Matrix4x4.CreateScale(-1, -1, 1));
        Singletons.Graphics.BindMat4(1, _camera.View);
        
        _mesh.Draw();
        
        Singletons.Graphics.EndRenderingFrame();
    }
}
