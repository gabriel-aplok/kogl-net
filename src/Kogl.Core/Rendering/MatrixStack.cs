using System.Numerics;

namespace Kogl.Core.Rendering;

/// <summary>The state of the matrix stack</summary>
public enum MatrixState
{
    ModelView,
    Projection,
}

/// <summary>A matrix stack that can be used to push and pop matrices</summary>
public class MatrixStack
{
    private readonly Stack<Matrix4x4> _modelViewStack = new(32);
    private readonly Stack<Matrix4x4> _projectionStack = new(32);

    private Matrix4x4 _modelView = Matrix4x4.Identity;
    private Matrix4x4 _projection = Matrix4x4.Identity;

    public Matrix4x4 ModelView => _modelView;
    public Matrix4x4 Projection => _projection;

    public MatrixState CurrentMode { get; set; } = MatrixState.ModelView;

    public ref Matrix4x4 CurrentMatrix =>
        ref (CurrentMode == MatrixState.ModelView ? ref _modelView : ref _projection);

    /// <summary>Pushes the current matrix onto the stack</summary>
    public void Push()
    {
        if (CurrentMode == MatrixState.ModelView)
        {
            _modelViewStack.Push(_modelView);
        }
        else
        {
            _projectionStack.Push(_projection);
        }
    }

    /// <summary>Pops the current matrix off the stack</summary>
    public void Pop()
    {
        if (CurrentMode == MatrixState.ModelView)
        {
            _modelView = _modelViewStack.Pop();
        }
        else
        {
            _projection = _projectionStack.Pop();
        }
    }

    /// <summary>Sets the current matrix to the identity</summary>
    public void LoadIdentity()
    {
        CurrentMatrix = Matrix4x4.Identity;
    }

    /// <summary>Sets the current matrix</summary>
    public void LoadMatrix(Matrix4x4 mat)
    {
        CurrentMatrix = mat;
    }

    /// <summary>Multiplies the current matrix</summary>
    public void Multiply(Matrix4x4 mat)
    {
        CurrentMatrix = mat * CurrentMatrix;
    }

    /// <summary>Scales the current matrix</summary>
    public void Scale(float x, float y, float z)
    {
        Multiply(Matrix4x4.CreateScale(x, y, z));
    }

    /// <summary>Translates the current matrix</summary>
    public void Translate(float x, float y, float z)
    {
        Multiply(Matrix4x4.CreateTranslation(x, y, z));
    }

    /// <summary>Rotates the current matrix</summary>
    public void Rotate(float angleDeg, float x, float y, float z)
    {
        Multiply(
            Matrix4x4.CreateFromAxisAngle(
                Vector3.Normalize(new Vector3(x, y, z)),
                angleDeg * (MathF.PI / 180f)
            )
        );
    }

    /// <summary>Rotates the current matrix</summary>
    public void Rotate(Quaternion quaternion)
    {
        Multiply(Matrix4x4.CreateFromQuaternion(quaternion));
    }

    /// <summary>Sets the current matrix to an orthographic projection</summary>
    public void Ortho(float left, float right, float bottom, float top, float zNear, float zFar)
    {
        Multiply(Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, zNear, zFar));
    }

    /// <summary>Sets the current matrix to a perspective projection</summary>
    public void Perspective(float fovy, float aspect, float zNear, float zFar)
    {
        Multiply(Matrix4x4.CreatePerspectiveFieldOfView(fovy, aspect, zNear, zFar));
    }

    /// <summary>Sets the current matrix to a perspective projection</summary>
    public void Frustum(float left, float right, float bottom, float top, float zNear, float zFar)
    {
        Multiply(Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, zNear, zFar));
    }
}
