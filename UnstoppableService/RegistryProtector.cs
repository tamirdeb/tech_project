using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace UnstoppableService;

public static class RegistryProtector
{
    private const string SafeBootKey = @"SYSTEM\CurrentControlSet\Control\SafeBoot";

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegOpenKeyEx(
        IntPtr hKey,
        string lpSubKey,
        int ulOptions,
        int samDesired,
        out IntPtr phkResult);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegCloseKey(IntPtr hKey);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint GetSecurityInfo(
        IntPtr handle,
        int objectType,
        int securityInfo,
        out IntPtr pSidOwner,
        out IntPtr pSidGroup,
        out IntPtr pDacl,
        out IntPtr pSacl,
        out IntPtr pSecurityDescriptor);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint SetSecurityInfo(
        IntPtr handle,
        int objectType,
        int securityInfo,
        IntPtr pSidOwner,
        IntPtr pSidGroup,
        IntPtr pDacl,
        IntPtr pSacl);

    public static void LockSafeBoot()
    {
        // This is a placeholder for the complex logic of modifying registry security.
        // A full implementation requires extensive P/Invoke and is omitted for brevity.
        Console.WriteLine("Registry key protection logic would be implemented here.");
    }
}
