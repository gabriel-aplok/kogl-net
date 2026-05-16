using System.Numerics;

namespace Kogl.Core;

public enum MatrixStackMode
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

    public MatrixStackMode CurrentMode { get; set; } = MatrixStackMode.ModelView;

    public ref Matrix4x4 CurrentMatrix =>
        ref (CurrentMode == MatrixStackMode.ModelView ? ref _modelView : ref _projection);

    /// <summary>
    /// Pushes the current matrix onto the stack
    /// </summary>
    public void Push()
    {
        if (CurrentMode == MatrixStackMode.ModelView)
        {
            _modelViewStack.Push(_modelView);
        }
        else
        {
            _projectionStack.Push(_projection);
        }
    }

    /// <summary>
    /// Pops the current matrix off the stack
    /// </summary>
    public void Pop()
    {
        if (CurrentMode == MatrixStackMode.ModelView)
        {
            _modelView = _modelViewStack.Pop();
        }
        else
        {
            _projection = _projectionStack.Pop();
        }
    }

    /// <summary>
    /// Sets the current matrix to the identity
    /// </summary>
    public void LoadIdentity()
    {
        CurrentMatrix = Matrix4x4.Identity;
    }

    /// <summary>
    /// Sets the current matrix
    /// </summary>
    /// <param name="mat">The matrix</param>
    public void LoadMatrix(Matrix4x4 mat)
    {
        CurrentMatrix = mat;
    }

    /// <summary>
    /// Multiplies the current matrix
    /// </summary>
    /// <param name="mat">The matrix</param>
    public void Multiply(Matrix4x4 mat)
    {
        CurrentMatrix = mat * CurrentMatrix;
    }

    /// <summary>
    /// Scales the current matrix
    /// </summary>
    /// <param name="x">The x scale</param>
    /// <param name="y">The y scale</param>
    /// <param name="z">The z scale</param>
    public void Scale(float x, float y, float z)
    {
        Multiply(Matrix4x4.CreateScale(x, y, z));
    }

    /// <summary>
    /// Translates the current matrix
    /// </summary>
    /// <param name="x">The x translation</param>
    /// <param name="y">The y translation</param>
    /// <param name="z">The z translation</param>
    public void Translate(float x, float y, float z)
    {
        Multiply(Matrix4x4.CreateTranslation(x, y, z));
    }

    /// <summary>
    /// Rotates the current matrix
    /// </summary>
    /// <param name="angleDeg">The angle in degrees</param>
    /// <param name="x">The x axis</param>
    /// <param name="y">The y axis</param>
    /// <param name="z">The z axis</param>
    public void Rotate(float angleDeg, float x, float y, float z)
    {
        Multiply(
            Matrix4x4.CreateFromAxisAngle(
                Vector3.Normalize(new Vector3(x, y, z)),
                angleDeg * (MathF.PI / 180f)
            )
        );
    }

    /// <summary>
    /// Sets the current matrix to an orthographic projection
    /// </summary>
    /// <param name="left">The left plane</param>
    /// <param name="right">The right plane</param>
    /// <param name="bottom">The bottom plane</param>
    /// <param name="top">The top plane</param>
    /// <param name="zNear">The near plane</param>
    /// <param name="zFar">The far plane</param>
    public void Ortho(float left, float right, float bottom, float top, float zNear, float zFar)
    {
        Multiply(Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, zNear, zFar));
    }

    /// <summary>
    /// Sets the current matrix to a perspective projection
    /// </summary>
    /// <param name="fovy">The field of view in y</param>
    /// <param name="aspect">The aspect ratio</param>
    /// <param name="zNear">The near plane</param>
    /// <param name="zFar">The far plane</param>
    public void Perspective(float fovy, float aspect, float zNear, float zFar)
    {
        Multiply(Matrix4x4.CreatePerspectiveFieldOfView(fovy, aspect, zNear, zFar));
    }

    /// <summary>
    /// Sets the current matrix to a perspective projection
    /// </summary>
    /// <param name="left">The left plane</param>
    /// <param name="right">The right plane</param>
    /// <param name="bottom">The bottom plane</param>
    /// <param name="top">The top plane</param>
    /// <param name="zNear">The near plane</param>
    /// <param name="zFar">The far plane</param>
    public void Frustum(float left, float right, float bottom, float top, float zNear, float zFar)
    {
        Multiply(Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, zNear, zFar));
    }
}
