namespace VoxelGame.Core.Components;

public interface ICamera
{
    public Mat4 Projection { get; }
    public Mat4 View { get; }
}