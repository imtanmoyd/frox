using System.Runtime.InteropServices;

namespace FROX.Core;

public enum ActivityState
{
    Active,
    Idle
}

public sealed class IdleWatcher : IDisposable
{
    private readonly Timer _timer;
    private readonly object _lock = new();
    private long _lastInputTicks;
    private bool _disposed;

    public IdleWatcher(int pollIntervalMs = 1000)
    {
        _lastInputTicks = QueryLastInputTicks();
        _timer = new Timer(_ => RefreshState(), null, pollIntervalMs, pollIntervalMs);
    }

    public event EventHandler<ActivityState>? StateChanged;

    public bool IsIdle => GetIdleTimeMs() >= 5000;

    public long GetIdleTimeMs()
    {
        var delta = DateTime.UtcNow.Ticks - Interlocked.Read(ref _lastInputTicks);
        return delta / TimeSpan.TicksPerMillisecond;
    }

    public void MarkUserActivity()
    {
        lock (_lock)
        {
            _lastInputTicks = DateTime.UtcNow.Ticks;
        }
    }

    private void RefreshState()
    {
        if (_disposed)
        {
            return;
        }

        // Query actual system-wide last input time
        var tickCount = QueryLastInputTicks();
        if (tickCount > Interlocked.Read(ref _lastInputTicks))
        {
            Interlocked.Exchange(ref _lastInputTicks, tickCount);
        }

        var idle = IsIdle;
        var state = idle ? ActivityState.Idle : ActivityState.Active;
        StateChanged?.Invoke(this, state);
    }

    private static long QueryLastInputTicks()
    {
        try
        {
            var plii = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
            if (GetLastInputInfo(ref plii))
            {
                // Environment.TickCount is the number of milliseconds since system start
                var systemUptimeMs = Environment.TickCount;
                var idleMs = systemUptimeMs - plii.dwTime;
                var idleTicks = TimeSpan.FromMilliseconds(idleMs).Ticks;
                return DateTime.UtcNow.Ticks - idleTicks;
            }
        }
        catch
        {
            // Fallback on failure
        }

        return DateTime.UtcNow.Ticks;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
}
