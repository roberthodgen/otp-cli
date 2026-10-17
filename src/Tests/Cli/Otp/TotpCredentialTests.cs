using Shouldly;
using Xunit;

namespace RobertHodgen.Otp.Cli.Tests.Otp;

public sealed class TotpCredentialTests
{
    [Fact]
    public void Parse_decodes_an_issuer_prefixed_label_for_cli_use()
    {
        var credential = TotpCredential.Parse("otpauth://totp/Example%20Issuer%3A%20rob?secret=JBSWY3DP");

        credential.Label.Value.ShouldBe("Example Issuer: rob");
    }

    [Fact]
    public void Create_keeps_the_supplied_label_when_issuer_is_present()
    {
        var credential = TotpCredential.Create(OtpLabel.Parse("rob"), "JBSWY3DP", null, null, "example.com");

        credential.Label.Value.ShouldBe("rob");
        credential.CanonicalUri.ShouldBe("otpauth://totp/rob?secret=JBSWY3DP&issuer=example.com");
    }

    [Fact]
    public void Create_omits_default_parameters_from_the_canonical_uri()
    {
        var credential = TotpCredential.Create(OtpLabel.Parse("rob"), "jbswy3dp", null, null, null);

        credential.CanonicalUri.ShouldBe("otpauth://totp/rob?secret=JBSWY3DP");
    }

    [Fact]
    public void Parse_ignores_unknown_extensions()
    {
        var credential = TotpCredential.Parse("otpauth://totp/rob?secret=JBSWY3DP&image=https%3A%2F%2Fexample.com");

        credential.CanonicalUri.ShouldBe("otpauth://totp/rob?secret=JBSWY3DP");
    }

    [Theory]
    [InlineData("otpauth://hotp/rob?secret=JBSWY3DP")]
    [InlineData("otpauth://totp/rob")]
    [InlineData("otpauth://totp/rob?secret=JBSWY3DP&SECRET=GEZDGNBV")]
    [InlineData("otpauth://totp/rob?secret=JBSWY3D!")]
    [InlineData("otpauth://totp/rob?secret=JBSWY3DP&algorithm=MD5")]
    [InlineData("otpauth://totp/rob?secret=JBSWY3DP&digits=7")]
    [InlineData("otpauth://totp/rob?secret=JBSWY3DP&period=0")]
    public void Parse_rejects_invalid_totp_configuration(string uri) =>
        Should.Throw<OtpValidationException>(() => TotpCredential.Parse(uri));
}
