using System;
using System.Threading;

public sealed class HotSwappableMobileService : IMobileService, IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private IMobileService _current;

    // One rebroadcast delegate per event so subscribers attach to the proxy only.
    private EventHandler<ConnectionArgs>? _rebroadcastConnected;
    private EventHandler<ConnectionArgs>? _rebroadcastDisconnected;

    public HotSwappableMobileService(IMobileService initial)
    {
        _current = initial;

        _rebroadcastConnected = (_, a) => Connected?.Invoke(this, a);
        _rebroadcastDisconnected = (_, a) => Disconnected?.Invoke(this, a);

        _current.Connected += _rebroadcastConnected;
        _current.Disconnected += _rebroadcastDisconnected;
    }

    public event EventHandler<ConnectionArgs>? Connected;
    public event EventHandler<ConnectionArgs>? Disconnected;

    public void Connect()
    {
        _lock.EnterReadLock();
        try { _current.Connect(); }
        finally { _lock.ExitReadLock(); }
    }

    public void Disconnect()
    {
        _lock.EnterReadLock();
        try { _current.Disconnect(); }
        finally { _lock.ExitReadLock(); }
    }

    public Location GetLocation()
    {
        _lock.EnterReadLock();
        try { return _current.GetLocation(); }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Swap to a new adapter (new vendor DLL/version) at runtime.</summary>
    public void SwapTo(Func<IMobileService> factory)
    {
        IMobileService? old = null, next = null;

        _lock.EnterWriteLock();
        try
        {
            next = factory(); // build first; if this throws, keep old running

            if (_rebroadcastConnected != null)
                next.Connected += _rebroadcastConnected;
            if (_rebroadcastDisconnected != null)
                next.Disconnected += _rebroadcastDisconnected;

            old = _current;
            _current = next;
        }
        finally { _lock.ExitWriteLock(); }

        // Detach & dispose old outside the lock to minimize pause
        if (old != null)
        {
            if (_rebroadcastConnected != null) old.Connected -= _rebroadcastConnected;
            if (_rebroadcastDisconnected != null) old.Disconnected -= _rebroadcastDisconnected;
            (old as IDisposable)?.Dispose();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public void Dispose()
    {
        _lock.EnterWriteLock();
        try
        {
            if (_rebroadcastConnected != null) _current.Connected -= _rebroadcastConnected;
            if (_rebroadcastDisconnected != null) _current.Disconnected -= _rebroadcastDisconnected;
            ( _current as IDisposable )?.Dispose();
            _rebroadcastConnected = null;
            _rebroadcastDisconnected = null;
        }
        finally
        {
            _lock.ExitWriteLock();
            _lock.Dispose();
        }
    }
}
