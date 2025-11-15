using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace UnstoppableService;

public static class ProcessProtector
{
    private const int DACL_SECURITY_INFORMATION = 0x00000004;

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetKernelObjectSecurity(
        IntPtr Handle,
        int securityInformation,
        [Out] byte[] pSecurityDescriptor,
        uint nLength,
        out uint lpnLengthNeeded);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool SetKernelObjectSecurity(
        IntPtr Handle,
        int securityInformation,
        byte[] pSecurityDescriptor);

    public static void Protect()
    {
        IntPtr processHandle = System.Diagnostics.Process.GetCurrentProcess().Handle;

        byte[] dacl = new byte[0];
        uint daclSize;

        GetKernelObjectSecurity(processHandle, DACL_SECURITY_INFORMATION, dacl, 0, out daclSize);
        dacl = new byte[daclSize];
        if (!GetKernelObjectSecurity(processHandle, DACL_SECURITY_INFORMATION, dacl, daclSize, out daclSize))
        {
            throw new Exception("Failed to get kernel object security");
        }

        var rawSecurityDescriptor = new RawSecurityDescriptor(dacl, 0);

        rawSecurityDescriptor.DiscretionaryAcl.InsertAce(
            0,
            new CommonAce(
                AceFlags.None,
                AceQualifier.AccessDenied,
                (int)0x0001, // PROCESS_TERMINATE
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                false,
                null));

        byte[] newDacl = new byte[rawSecurityDescriptor.BinaryLength];
        rawSecurityDescriptor.GetBinaryForm(newDacl, 0);

        if (!SetKernelObjectSecurity(processHandle, DACL_SECURITY_INFORMATION, newDacl))
        {
            throw new Exception("Failed to set kernel object security");
        }
    }
}
