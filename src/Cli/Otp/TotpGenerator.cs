namespace RobertHodgen.Otp.Cli.Otp;

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

using Time;

public static class TotpGenerator
{
    public static string Generate(TotpCredential credential, OtpTimestamp timestamp)
    {
        var seconds = new DateTimeOffset(timestamp.Value).ToUnixTimeSeconds();
        var counter = seconds / credential.Period;
        Span<byte> counterBytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);
        var secret = DecodeBase32(credential.Secret);
        var hash = ComputeHash(credential.Algorithm, secret, counterBytes);
        var offset = hash[^1] & 0x0f;
        var binaryCode = ((hash[offset] & 0x7f) << 24) |
                         (hash[offset + 1] << 16) |
                         (hash[offset + 2] << 8) |
                         hash[offset + 3];
        var modulo = credential.Digits == 6 ? 1_000_000 : 100_000_000;
        return (binaryCode % modulo).ToString($"D{credential.Digits}", CultureInfo.InvariantCulture);
    }

    private static byte[] ComputeHash(string algorithm, byte[] secret, ReadOnlySpan<byte> counter) => algorithm switch
    {
        "SHA1" => HMACSHA1.HashData(secret, counter),
        "SHA256" => HMACSHA256.HashData(secret, counter),
        "SHA512" => HMACSHA512.HashData(secret, counter),
        _ => throw new OtpValidationException("The OTP algorithm is not supported."),
    };

    private static byte[] DecodeBase32(string value)
    {
        var bytes = new List<byte>();
        var buffer = 0;
        var bits = 0;

        foreach (var character in value)
        {
            var digit = character is >= 'A' and <= 'Z' ? character - 'A' : character - '2' + 26;
            buffer = (buffer << 5) | digit;
            bits += 5;

            while (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)(buffer >> bits));
                buffer &= (1 << bits) - 1;
            }
        }

        return bytes.ToArray();
    }
}
