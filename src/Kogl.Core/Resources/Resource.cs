namespace Kogl.Core.Resources;

public abstract class Resource : IDisposable
{
    public string Name { get; internal set; } = string.Empty;
    public string Path { get; internal set; } = string.Empty;
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        Dispose(true);
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    protected abstract void Dispose(bool disposing);

    ~Resource()
    {
        Dispose(false);
    }
}
