using AdysTech.CredentialManager;
using PuppyWorkbooks.Integration.Models;

namespace PuppyWorkbooks.Integration.Engine;

public sealed class WindowsCredentialStoreSecretManager
{
    public static string GetWindowsCredential(WindowsCredentialsSecret win, string secretName)
    {
        var credential = CredentialManager.GetCredentials(win.CredentialTargetName);
        if (credential == null)
            throw new InvalidOperationException($"Windows credential '{win.CredentialTargetName}' not found.");

        if (string.Equals(secretName, win.UsernameToSecretName, StringComparison.OrdinalIgnoreCase))
            return credential.UserName;
        if (string.Equals(secretName, win.PasswordToSecretName, StringComparison.OrdinalIgnoreCase))
            return credential.Password;

        throw new InvalidOperationException($"Secret '{secretName}' does not map to username or password for Windows credential '{win.CredentialTargetName}'.");
    }
}