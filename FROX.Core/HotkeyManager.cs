using System.Runtime.InteropServices;
using System.Windows.Input;

namespace FROX.Core;

public sealed class HotkeyManager : IDisposable
{
    private readonly Dictionary<int, Action> _handlers = new();
    private readonly IntPtr _windowHandle;
    private int _nextId;

    public HotkeyManager(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public void Register(Key key, ModifierKeys modifiers, Action handler)
    {
        var id = ++_nextId;
        var vk = KeyInterop.VirtualKeyFromKey(key);
        var mod = (uint)modifiers;

        if (!RegisterHotKey(_windowHandle, id, mod, (uint)vk))
        {
            throw new InvalidOperationException($"Unable to register hotkey for {key} + {modifiers}.");
        }

        _handlers[id] = handler;
    }

    public void HandleMessage(int msg, IntPtr wParam)
    {
        if (msg != WM_HOTKEY)
        {
            return;
        }

        var id = wParam.ToInt32();
        if (_handlers.TryGetValue(id, out var handler))
        {
            handler();
        }
    }

    public void Dispose()
    {
        foreach (var id in _handlers.Keys.ToList())
        {
            UnregisterHotKey(_windowHandle, id);
        }

        _handlers.Clear();
    }

    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
