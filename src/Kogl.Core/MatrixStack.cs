using System.Numerics;

namespace Kogl.Core;

public enum MatrixMode
{
    ModelView,
    Projection,
}

public class MatrixStack
{
    private readonly Stack<Matrix4x4> _modelViewStack = new(32);
    private readonly Stack<Matrix4x4> _projectionStack = new(32);

    private Matrix4x4 _modelView = Matrix4x4.Identity;
    private Matrix4x4 _projection = Matrix4x4.Identity;

    public Matrix4x4 ModelView => _modelView;
    public Matrix4x4 Projection => _projection;

    public MatrixMode CurrentMode { get; set; } = MatrixMode.ModelView;

    public ref Matrix4x4 CurrentMatrix =>
        ref (CurrentMode == MatrixMode.ModelView ? ref _modelView : ref _projection);

    public void Push()
    {
        if (CurrentMode == MatrixMode.ModelView)
            _modelViewStack.Push(_modelView);
        else
            _projectionStack.Push(_projection);
    }

    public void Pop()
    {
        if (CurrentMode == MatrixMode.ModelView)
            _modelView = _modelViewStack.Pop();
        else
            _projection = _projectionStack.Pop();
    }

    public void LoadIdentity()
    {
        CurrentMatrix = Matrix4x4.Identity;
    }

    public void Multiply(Matrix4x4 mat)
    {
        CurrentMatrix = mat * CurrentMatrix;
    }

    public void Translate(float x, float y, float z)
    {
        Multiply(Matrix4x4.CreateTranslation(x, y, z));
    }

    public void Rotate(float angleDeg, float x, float y, float z)
    {
        Multiply(
            Matrix4x4.CreateFromAxisAngle(
                Vector3.Normalize(new Vector3(x, y, z)),
                angleDeg * (MathF.PI / 180f)
            )
        );
    }

    public void Scale(float x, float y, float z)
    {
        Multiply(Matrix4x4.CreateScale(x, y, z));
    }

    public void Ortho(float left, float right, float bottom, float top, float zNear, float zFar)
    {
        Multiply(Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, zNear, zFar));
    }

    public void Perspective(float fovy, float aspect, float zNear, float zFar)
    {
        Multiply(Matrix4x4.CreatePerspectiveFieldOfView(fovy, aspect, zNear, zFar));
    }

    public void Frustum(float left, float right, float bottom, float top, float zNear, float zFar)
    {
        Multiply(Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, zNear, zFar));
    }
}
