namespace Kogl.Common;

public abstract class Disposable : IDisposable
{
    private volatile int _disposed;
    private readonly string _typeName;

    protected Disposable()
    {
        _typeName = GetType().Name;
    }

    public bool HasDisposed => _disposed != 0;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        else
        {
            LogCat.Warn($"Object of type [{_typeName}] has already been disposed.");
        }
    }

    protected abstract void Dispose(bool disposing);

    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
    }

    ~Disposable()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Dispose(false);
        }
    }
}
