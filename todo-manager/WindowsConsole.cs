using System;
using System.Runtime.InteropServices;

public enum WindowsOutputConsoleMode : uint
{
    ENABLE_PROCESSED_OUTPUT = 0x0001,
    ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
    ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
    ENABLE_LVB_GRID_WORLDWIDE = 0x0010,
    DISABLE_NEWLINE_AUTO_RETURN = 0x0008,
}

public static class WindowsConsole
{
    const int STD_OUTPUT_HANDLE = -11;

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    public static void WC_RawGetOutputConsoleMode(out uint mode)
    {
        var handle = GetStdHandle(STD_OUTPUT_HANDLE);
        GetConsoleMode(handle, out mode);
    }

    public static bool WC_AddOutputConsoleMode(WindowsOutputConsoleMode arg)
    {
        IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
        uint mode;
        GetConsoleMode(handle, out mode);
        mode |= ((uint)arg);
        return SetConsoleMode(handle, mode);
    }
    public static bool WC_RemoveOutputConsoleMode(WindowsOutputConsoleMode arg)
    {
        IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
        uint mode;
        GetConsoleMode(handle, out mode);
        mode &= ~((uint)arg);
        return SetConsoleMode(handle, mode);
    }
}