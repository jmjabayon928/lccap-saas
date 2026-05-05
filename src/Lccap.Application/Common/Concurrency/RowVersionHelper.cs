using System.Security.Cryptography;

namespace Lccap.Application.Common.Concurrency;

public static class RowVersionHelper
{
    public static bool IsMissingOrEmpty(byte[]? rowVersion)
    {
        return rowVersion == null || rowVersion.Length == 0;
    }

    public static byte[] NewRowVersion()
    {
        var bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    public static string ToBase64(byte[]? rowVersion)
    {
        if (rowVersion == null || rowVersion.Length == 0)
        {
            return string.Empty;
        }

        return Convert.ToBase64String(rowVersion);
    }

    public static byte[] FromBase64(string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            return Array.Empty<byte>();
        }

        try
        {
            return Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            return Array.Empty<byte>();
        }
    }
}
