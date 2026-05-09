using System.Security.Cryptography;
using System.Text;

namespace DailyPlanner.Services;

/// <summary>
/// Wraps Windows DPAPI for at-rest encryption of API tokens. Encrypted blobs are
/// stored as `enc::{base64}`. Plaintext (no prefix) is treated as legacy data
/// and returned as-is from Unprotect to allow one-time upgrade through the
/// settings save path.
///
/// Protect throws on encryption failure rather than falling back to plaintext —
/// silently storing tokens unencrypted defeats the purpose of having this class.
/// </summary>
public static class ProtectedTokenStore
{
    private const string Prefix = "enc::";

    public static string Protect(string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return string.Empty;
        if (plain.StartsWith(Prefix, StringComparison.Ordinal)) return plain;

        // Fail-fast: callers must not silently store unencrypted secrets when DPAPI
        // is unavailable. The original silent fallback meant a misconfigured
        // machine (e.g. running under a service account without a user profile)
        // would persist plaintext tokens to disk indefinitely without any signal.
        try
        {
            var bytes = Encoding.UTF8.GetBytes(plain);
            var encrypted = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(encrypted);
        }
        catch (Exception ex)
        {
            Log.Error("ProtectedTokenStore", $"Protect failed: {ex.Message}");
            throw new CryptographicException(
                "Could not encrypt token using DPAPI. Refusing to persist unencrypted secret.", ex);
        }
    }

    /// <summary>
    /// Decrypts an encrypted blob, or passes through legacy plaintext.
    /// Returns empty string if the blob is malformed/unrecoverable — callers
    /// then see "no token configured" and re-prompt the user, which is safer
    /// than throwing during settings load.
    /// </summary>
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
            // Logged loudly so the user sees their token is gone in the next
            // settings page render, rather than guessing why sync silently
            // returns 0 cards.
            Log.Error("ProtectedTokenStore",
                $"Unprotect failed (DPAPI key likely changed — Windows reinstall, profile reset, or different user). " +
                $"Token cleared, user must re-enter. Detail: {ex.Message}");
            return string.Empty;
        }
    }
}