using System;
using System.Collections.Generic;

namespace Nag0mi.Gunbreaker.Control;

internal sealed class WindowLease<T>(T original, IReadOnlyList<T> owned, Func<T, bool> contains,
    Action<T> add, Action<T> remove, Func<T, bool> isOpen, Action<T, bool> setOpen) : IDisposable where T : class
{
    private bool _active;
    private bool _disposed;
    private bool _originalRegistered;
    private bool _originalOpen;

    public void Activate()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_active) return;

        _originalRegistered = contains(original);
        _originalOpen = isOpen(original);
        _active = true;

        try
        {
            if (_originalRegistered) remove(original);
            setOpen(original, false);
            foreach (var window in owned)
                if (!contains(window)) add(window);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_active) return;
        _active = false;

        try
        {
            foreach (var window in owned)
            {
                if (contains(window)) remove(window);
                setOpen(window, false);
            }
        }
        finally
        {
            // The host may have changed visibility or restored registration while leased.
            try
            {
                if (_originalRegistered && !contains(original)) add(original);
            }
            finally
            {
                setOpen(original, _originalOpen);
            }
        }
    }
}
