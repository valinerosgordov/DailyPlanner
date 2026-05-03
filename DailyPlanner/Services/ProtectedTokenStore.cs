using System.Security.Cryptography;
using System.Text;

namespace DailyPlanner.Services;

public static class ProtectedTokenStore
{
    private const string Prefix = "enc::";

    public static string Protect(string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return string.Empty;
        if (plain.StartsWith(Prefix, StringComparison.Ordinal)) return plain;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(plain);
            var encrypted = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            Log.Error("ProtectedTokenStore", $"Protect failed: {ex.Message}");
            return plain;
        }
    }

    public static string Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return string.Empty;
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal)) return stored;
        try
        {
            var encrypted = Convert.FromBase64String(stored[Prefix.Length..]);
            var bytes = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            Log.Error("ProtectedTokenStore", $"Unprotect failed: {ex.Message}");
            return string.Empty;
        }
    }
}
