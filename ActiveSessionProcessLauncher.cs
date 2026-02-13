using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PickleRick;

public static class ActiveSessionProcessLauncher
{
    private const uint CreateUnicodeEnvironment = 0x00000400;
    private const uint TokenAssignPrimary = 0x0001;
    private const uint TokenDuplicate = 0x0002;
    private const uint TokenQuery = 0x0008;
    private const uint TokenAdjustDefault = 0x0080;
    private const uint TokenAdjustSessionId = 0x0100;

    public static bool TryLaunch(string executablePath, string arguments, ILogger logger, out int processId)
    {
        processId = 0;

        var sessionId = WTSGetActiveConsoleSessionId();
        if (sessionId == 0xFFFFFFFF)
        {
            logger.LogWarning("No active console session found; skipping playback.");
            return false;
        }

        if (!WTSQueryUserToken(sessionId, out var userToken))
        {
            logger.LogWarning("Failed to query user token: {error}", new Win32Exception(Marshal.GetLastWin32Error()).Message);
            return false;
        }

        using var tokenHandle = new SafeHandleWrapper(userToken);
        var tokenAccess = TokenAssignPrimary | TokenDuplicate | TokenQuery | TokenAdjustDefault | TokenAdjustSessionId;
        if (!DuplicateTokenEx(userToken, tokenAccess, IntPtr.Zero, 2, 1, out var primaryToken))
        {
            logger.LogWarning("Failed to duplicate user token: {error}", new Win32Exception(Marshal.GetLastWin32Error()).Message);
            return false;
        }

        using var primaryHandle = new SafeHandleWrapper(primaryToken);
        if (!CreateEnvironmentBlock(out var environment, primaryToken, false))
        {
            logger.LogWarning("Failed to create environment block: {error}", new Win32Exception(Marshal.GetLastWin32Error()).Message);
            return false;
        }

        try
        {
            var startupInfo = new STARTUPINFO
            {
                cb = Marshal.SizeOf<STARTUPINFO>(),
                lpDesktop = "winsta0\\default"
            };

            var commandLine = $"\"{executablePath}\" {arguments}";
            if (!CreateProcessAsUser(
                    primaryToken,
                    executablePath,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    CreateUnicodeEnvironment,
                    environment,
                    Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory,
                    ref startupInfo,
                    out var processInfo))
            {
                logger.LogWarning("Failed to launch player: {error}", new Win32Exception(Marshal.GetLastWin32Error()).Message);
                return false;
            }

            CloseHandle(processInfo.hThread);
            CloseHandle(processInfo.hProcess);
            processId = processInfo.dwProcessId;
            return true;
        }
        finally
        {
            DestroyEnvironmentBlock(environment);
        }
    }

    private sealed class SafeHandleWrapper : IDisposable
    {
        private IntPtr _handle;

        public SafeHandleWrapper(IntPtr handle)
        {
            _handle = handle;
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool DuplicateTokenEx(
        IntPtr existingToken,
        uint desiredAccess,
        IntPtr tokenAttributes,
        int impersonationLevel,
        int tokenType,
        out IntPtr newToken);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out IntPtr environment, IntPtr token, bool inherit);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool DestroyEnvironmentBlock(IntPtr environment);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessAsUser(
        IntPtr token,
        string applicationName,
        string commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string currentDirectory,
        ref STARTUPINFO startupInfo,
        out PROCESS_INFORMATION processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}

