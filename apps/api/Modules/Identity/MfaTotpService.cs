using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace MunicipalPlatform.Api.Modules.Identity;

public sealed class MfaTotpService(IDataProtectionProvider dataProtectionProvider)
{
    private const int StepSeconds = 30;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("MunicipalPlatform.Identity.MfaSecret.v1");

    public MfaEnrollment CreateEnrollment(string issuer, string username)
    {
        var secret = RandomNumberGenerator.GetBytes(20);
        var encoded = Base32Encode(secret);
        var protectedSecret = _protector.Protect(encoded);
        var label = Uri.EscapeDataString($"{issuer}:{username}");
        var uri = $"otpauth://totp/{label}?secret={encoded}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period={StepSeconds}";
        return new MfaEnrollment(encoded, protectedSecret, uri);
    }

    public bool VerifyProtectedSecret(string protectedSecret, string? code, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || code.Any(c => !char.IsAsciiDigit(c))) return false;
        string encoded;
        try { encoded = _protector.Unprotect(protectedSecret); }
        catch (CryptographicException) { return false; }
        var secret = Base32Decode(encoded);
        var counter = now.ToUnixTimeSeconds() / StepSeconds;
        for (var offset = -1; offset <= 1; offset++)
        {
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(GenerateCode(secret, counter + offset)), Encoding.ASCII.GetBytes(code))) return true;
        }
        return false;
    }

    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "RFC 6238 TOTP interoperability uses HMAC-SHA1 by default; this keyed MAC use does not rely on SHA-1 collision resistance.")]
    internal static string GenerateCode(byte[] secret, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);
        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static string Base32Encode(ReadOnlySpan<byte> data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0; var bitsLeft = 0;
        foreach (var value in data)
        {
            buffer = (buffer << 8) | value; bitsLeft += 8;
            while (bitsLeft >= 5) { bitsLeft -= 5; output.Append(alphabet[(buffer >> bitsLeft) & 31]); }
        }
        if (bitsLeft > 0) output.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);
        return output.ToString();
    }

    private static byte[] Base32Decode(string value)
    {
        var clean = value.Trim().TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>(); var buffer = 0; var bitsLeft = 0;
        foreach (var character in clean)
        {
            var index = character switch { >= 'A' and <= 'Z' => character - 'A', >= '2' and <= '7' => character - '2' + 26, _ => throw new FormatException("Segredo MFA Base32 inválido.") };
            buffer = (buffer << 5) | index; bitsLeft += 5;
            if (bitsLeft >= 8) { bitsLeft -= 8; bytes.Add((byte)((buffer >> bitsLeft) & 255)); }
        }
        return bytes.ToArray();
    }
}

public sealed record MfaEnrollment(string Secret, string ProtectedSecret, string OtpAuthUri);
