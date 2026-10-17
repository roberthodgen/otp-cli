using Shouldly;
using Xunit;

namespace RobertHodgen.Otp.Cli.Tests.Otp;

public sealed class TotpGeneratorTests
{
    [Theory]
    [InlineData("SHA1", "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", "94287082")]
    [InlineData("SHA256", "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA", "46119246")]
    [InlineData("SHA512", "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNA", "90693936")]
    public void Generate_matches_rfc_6238_at_59_seconds(string algorithm, string secret, string expected)
    {
        var credential = TotpCredential.Parse(
            $"otpauth://totp/rfc?secret={secret}&algorithm={algorithm}&digits=8");

        TotpGenerator.Generate(credential, OtpTimestamp.FromTime(DateTime.UnixEpoch.AddSeconds(59)))
            .ShouldBe(expected);
    }

    [Fact]
    public void Generate_uses_the_configured_period_boundary()
    {
        var credential = TotpCredential.Parse("otpauth://totp/rfc?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&period=60");

        TotpGenerator.Generate(credential, OtpTimestamp.FromTime(DateTime.UnixEpoch.AddSeconds(59)))
            .ShouldBe("755224");
    }
}
