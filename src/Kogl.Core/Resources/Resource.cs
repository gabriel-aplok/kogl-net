using Kogl.Common;

namespace Kogl.Core.Resources;

public abstract class Resource : Disposable
{
    public ulong UniqueId { get; internal set; } = 0;
    public string Name { get; internal set; } = string.Empty;
    public string Path { get; internal set; } = string.Empty;

    public bool IsDisposed => HasDisposed;

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        DisposeManaged();
    }

    /// <summary>Override this method to release managed resources.</summary>
    protected virtual void DisposeManaged() { }
}
