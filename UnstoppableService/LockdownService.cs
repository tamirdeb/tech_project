using Microsoft.Win32;
using System.Security.AccessControl;
using System.Security.Principal;

namespace UnstoppableService;

public class LockdownService
{
    private const string SafeBootKeyPath = @"SYSTEM\CurrentControlSet\Control\SafeBoot";

    public void EnableLockdown()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(SafeBootKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership);
            if (key == null)
            {
                throw new Exception($"Failed to open registry key: {SafeBootKeyPath}");
            }

            var acl = key.GetAccessControl();

            // Take ownership
            var currentUser = WindowsIdentity.GetCurrent().User;
            acl.SetOwner(currentUser);
            key.SetAccessControl(acl);

            // Disable inheritance and remove existing rules
            acl.SetAccessRuleProtection(true, true);
            key.SetAccessControl(acl);

            // Grant SYSTEM full control
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            acl.AddAccessRule(new RegistryAccessRule(systemSid, RegistryRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));

            // Deny write access to Administrators
            var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            acl.AddAccessRule(new RegistryAccessRule(adminSid, RegistryRights.WriteKey | RegistryRights.Delete, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Deny));

            key.SetAccessControl(acl);
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error enabling lockdown: {ex.Message}");
        }
    }

    public void DisableLockdown()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(SafeBootKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions);
             if (key == null)
            {
                throw new Exception($"Failed to open registry key: {SafeBootKeyPath}");
            }

            var acl = key.GetAccessControl();

            // Restore write access to Administrators
            var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            acl.RemoveAccessRule(new RegistryAccessRule(adminSid, RegistryRights.WriteKey | RegistryRights.Delete, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Deny));

            // Re-enable inheritance
            acl.SetAccessRuleProtection(false, false);

            key.SetAccessControl(acl);
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error disabling lockdown: {ex.Message}");
        }
    }
}
