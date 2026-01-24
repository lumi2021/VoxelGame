namespace VoxelGame.Core.Components;

public class MeshBuilder
{
    
    private List<uint> _indices = [];
    private List<Vec3> _vertices = [];
    private List<Vec2> _uvs = [];
    public int TriangleCount => _indices.Count / 3;
    
    public void AppendMeshData(
        uint[] indices,
        Vec3[] vertices,
        Vec2[] uvs)
    {
        var triangleOffset = (uint)_vertices.Count;
        foreach (var i in indices) _indices.Add(i + triangleOffset);
        _vertices.AddRange(vertices);
        _uvs.AddRange(uvs);
    }

    public void AddTriangle(
        Vec3 v1, Vec3 v2, Vec3 v3,
        Vec2 uv1, Vec2 uv2, Vec2 uv3
        )
    {
        var triangleOffset = (uint)_vertices.Count;
        _indices.Add(triangleOffset + 0);
        _indices.Add(triangleOffset + 1);
        _indices.Add(triangleOffset + 2);
        
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        
        _uvs.Add(uv1);
        _uvs.Add(uv2);
        _uvs.Add(uv3);
    }
    
    public void AddQuad(
        Vec3 v1, Vec3 v2, Vec3 v3, Vec3 v4,
        Vec2 uv1, Vec2 uv2, Vec2 uv3, Vec2 uv4
    )
    {
        var triangleOffset = (uint)_vertices.Count;
        
        _indices.Add(triangleOffset + 2);
        _indices.Add(triangleOffset + 1);
        _indices.Add(triangleOffset + 0);
        
        _indices.Add(triangleOffset + 3);
        _indices.Add(triangleOffset + 2);
        _indices.Add(triangleOffset + 0);
        
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _vertices.Add(v4);
        
        _uvs.Add(uv1);
        _uvs.Add(uv2);
        _uvs.Add(uv3);
        _uvs.Add(uv4);
    }
    
    public void Commit(Mesh mesh)
    {
        mesh.IndexBuffer.Fetch([.. _indices]);
        mesh.VertexPositionBuffer.Fetch([.. _vertices]);
        mesh.VertexCoordBuffer.Fetch([.. _uvs]);
    }
}
