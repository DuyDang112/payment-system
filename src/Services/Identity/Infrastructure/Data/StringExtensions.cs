using System.Security.Cryptography;
using System.Text;

namespace Identity.Infrastructure.Data;

/// <summary>
/// Extension methods for string operations
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to a SHA256 hash suitable for Duende IdentityServer secrets
    /// </summary>
    public static string Sha256(this string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}