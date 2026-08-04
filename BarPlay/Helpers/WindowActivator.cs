using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BarPlay.Helpers;

internal static partial class WindowActivator
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const int ShowWindowRestore = 9;
    private const byte VirtualKeyMenu = 0x12;
    private const uint KeyEventKeyUp = 0x02;

    private const int MaxApplicationUserModelIdLength = 130;

    private static readonly EnumWindowsProc s_enumWindowsCallback = OnEnumWindows;
    private static readonly List<nint> s_windowHandles = [];

    private delegate bool EnumWindowsProc(nint windowHandle, nint lParam);

    public static bool TryActivateRunningInstance(string appUserModelId)
    {
        var targetProcessIds = FindProcessIdsByApplicationUserModelId(appUserModelId);
        if (targetProcessIds.Count == 0) return false;

        var windowHandle = FindVisibleWindow(targetProcessIds);
        if (windowHandle == 0) return false;

        if (IsIconic(windowHandle)) _ = ShowWindow(windowHandle, ShowWindowRestore);

        if (SetForegroundWindow(windowHandle)) return true;

        // Windows' foreground lock rejects SetForegroundWindow when the caller does not own the
        // foreground (e.g. the user is focused on another app). Synthesizing an Alt key-up makes
        // the OS treat the caller as the active input source, briefly releasing the lock so the
        // target window can be raised. This is a well-known workaround, not real key input: the
        // synthesized event is never delivered to the target window and cannot disturb typing.
        keybd_event(VirtualKeyMenu, 0, KeyEventKeyUp, 0);
        _ = SetForegroundWindow(windowHandle);
        return true;
    }

    private static HashSet<uint> FindProcessIdsByApplicationUserModelId(string appUserModelId)
    {
        var matchingProcessIds = new HashSet<uint>();

        // Enumeration may touch protected processes, so failures are ignored per process.
        try
        {
            foreach (var process in Process.GetProcesses())
            {
                var processHandle = OpenProcess(ProcessQueryLimitedInformation, false, (uint)process.Id);
                if (processHandle == 0) continue;

                try
                {
                    if (!TryGetApplicationUserModelId(processHandle, out var processApplicationUserModelId)) continue;
                    if (string.Equals(processApplicationUserModelId, appUserModelId, StringComparison.OrdinalIgnoreCase)) matchingProcessIds.Add((uint)process.Id);
                }
                finally { _ = CloseHandle(processHandle); }
            }
        }
        catch { }

        return matchingProcessIds;
    }

    private static nint FindVisibleWindow(HashSet<uint> targetProcessIds)
    {
        List<nint> windows;
        lock (s_windowHandles)
        {
            s_windowHandles.Clear();
            _ = EnumWindows(s_enumWindowsCallback, 0);
            windows = [.. s_windowHandles];
        }

        nint firstVisibleWindowHandle = 0;
        foreach (var windowHandle in windows)
        {
            _ = GetWindowThreadProcessId(windowHandle, out var processId);
            if (!targetProcessIds.Contains(processId)) continue;
            if (!IsWindowVisible(windowHandle)) continue;

            if (firstVisibleWindowHandle == 0) firstVisibleWindowHandle = windowHandle;
            if (!IsIconic(windowHandle)) return windowHandle;
        }

        return firstVisibleWindowHandle;
    }

    private static bool TryGetApplicationUserModelId(nint processHandle, out string? applicationUserModelId)
    {
        Span<char> buffer = stackalloc char[MaxApplicationUserModelIdLength];
        var bufferLength = (uint)buffer.Length;

        unsafe
        {
            fixed (char* bufferPointer = buffer)
            {
                if (GetApplicationUserModelId(processHandle, ref bufferLength, bufferPointer) != 0)
                {
                    applicationUserModelId = null;
                    return false;
                }
            }
        }

        applicationUserModelId = new string(buffer[..(int)bufferLength]).TrimEnd('\0');
        return true;
    }

    private static bool OnEnumWindows(nint windowHandle, nint lParam)
    {
        lock (s_windowHandles) s_windowHandles.Add(windowHandle);
        return true;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProc callback, nint lParam);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint windowHandle, out uint processId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(nint windowHandle);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(nint windowHandle);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(nint windowHandle, int command);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(nint windowHandle);

    // Simulates a single Alt key-up to work around the Windows foreground lock (see TryActivateRunningInstance).
    // The generated input is not delivered to any window, so it has no effect on the user's own keyboard input.
    [LibraryImport("user32.dll")]
    private static partial void keybd_event(byte virtualKey, byte scanCode, uint flags, nuint extraInfo);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint handle);

    [LibraryImport("kernel32.dll")]
    private static unsafe partial int GetApplicationUserModelId(nint processHandle, ref uint applicationUserModelIdLength, char* applicationUserModelId);
}
