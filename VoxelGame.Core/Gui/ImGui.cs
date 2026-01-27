using VoxelGame.Core.Components;
using VoxelGame.Core.Data.Graphics;

namespace VoxelGame.Core.gui;

public static class ImGui
{

    private static IMaterial _mat;
    private static Mesh _quad;
    
    public static void Setup()
    {
        _mat = Singletons.Graphics.GenerateMaterial(new MaterialOptions(
            "shaders/gui.vert.spv",
            "shaders/gui.frag.spv")
            {
                VertAttributes = [MaterialType.Vec3, MaterialType.Vec2],
                VertUniforms = [MaterialType.Mat4, MaterialType.Mat4],
            });

        _quad = new Mesh() { Material =  _mat };
        var mb = new MeshBuilder();
        mb.AddQuad(
            new Vec3(0, 0, 0), new Vec3(1, 0, 0),  new Vec3(1, 1, 0), new Vec3(0, 1, 0),
            new Vec2(0, 0), new Vec2(1, 0), new Vec2(1, 1), new Vec2(0, 1));
        mb.Commit(_quad);
    }

    public static void BeginFrame()
    {
        var projection = Mat4.CreateOrthographicOffCenter(0, 800, 0, 600, 0, 1);
        var model = Mat4.CreateScale(500, 300, 0);
        
        _quad.GetRenderContext()?
            .WithVertexUniform(0, projection)
            .WithVertexUniform(1, model)
            .Draw();
    }

    public static void EndFrame()
    {
        
    }
    
}
