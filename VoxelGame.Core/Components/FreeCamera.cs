using VoxelGame.Core.Data.Input;
using VoxelGame.Core.Math;
using CSMath = System.Math;

namespace VoxelGame.Core.Components;

public class FreeCamera: ICamera, IUpdateProcess
{
    public Vec3 Position;
    public Vec3 Rotation;
    
    public float FieldOfView = 90; 
    public float NearPlane = 0.001f;
    public float FarPlane = 10000f;
    
    public Mat4 Projection =>  Mat4.CreatePerspectiveFieldOfView(
            Deg2Rad(FieldOfView),
            Singletons.Graphics.ViewportSize.X / (float)Singletons.Graphics.ViewportSize.Y,
            NearPlane, FarPlane);

    public Mat4 View => Mat4.CreateLookTo(
        Position,
        DirectionFromRotation(Rotation),
        Vec3.UnitY);

    public void Update(double delta)
    {
        var input = Singletons.Input;
        
        const float sensitivity = .5f;
        const float speed = 5f;

        var baseMovementDirection = new Vec3();
        if (input.IsPressed(Key.S)) baseMovementDirection.Z -= 1f;
        if (input.IsPressed(Key.W)) baseMovementDirection.Z += 1f;
        if (input.IsPressed(Key.A)) baseMovementDirection.X -= 1f;
        if (input.IsPressed(Key.D)) baseMovementDirection.X += 1f;
        if (input.IsPressed(Key.Space)) baseMovementDirection.Y += 1f;
        if (input.IsPressed(Key.ShiftLeft)) baseMovementDirection.Y -= 1f;
        
        Rotation.X += input.CursorOffset.Y * sensitivity;
        Rotation.Y += input.CursorOffset.X * sensitivity;
        Rotation.Z = 0;

        Rotation.X = CSMath.Clamp(Rotation.X, -89.9f, 89.9f);
        
        var direction = baseMovementDirection.RotateBy(Rotation.X, Rotation.Y);
        Position += direction.MulScalar(delta * speed);
    }
}

