using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MTPhotosWallpaper.Services;

public class CredentialService
{
    private const string TargetName = "MT-Photos Wallpaper";

    public void StorePassword(string username, string password)
    {
        var credential = new CREDENTIAL
        {
            Type = CRED_TYPE.GENERIC,
            TargetName = TargetName,
            CredentialBlob = Marshal.StringToCoTaskMemUni(password),
            CredentialBlobSize = (uint)(password.Length * 2),
            Persist = CRED_PERSIST.LOCAL_MACHINE,
            UserName = username
        };

        if (!CredWrite(ref credential, 0))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public (string? Username, string? Password) GetCredentials()
    {
        if (!CredRead(TargetName, CRED_TYPE.GENERIC, 0, out var credentialPtr))
        {
            return (null, null);
        }

        try
        {
            var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
            var password = Marshal.PtrToStringUni(credential.CredentialBlob, (int)(credential.CredentialBlobSize / 2));
            return (credential.UserName, password);
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }

    public void ClearPassword()
    {
        CredDelete(TargetName, CRED_TYPE.GENERIC, 0);
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, CRED_TYPE type, uint reservedFlag, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, CRED_TYPE type, uint flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public CRED_TYPE Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CRED_PERSIST Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    private enum CRED_TYPE : uint
    {
        GENERIC = 1,
        DOMAIN_PASSWORD = 2,
        DOMAIN_CERTIFICATE = 3,
        DOMAIN_VISIBLE_PASSWORD = 4,
        GENERIC_CERTIFICATE = 5,
        DOMAIN_EXTENDED = 6,
        MAXIMUM = 7
    }

    private enum CRED_PERSIST : uint
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3
    }
}
