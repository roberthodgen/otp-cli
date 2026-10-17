using Shouldly;
using Xunit;

namespace RobertHodgen.Otp.Cli.Tests.Otp;

public sealed class OtpLabelTests
{
    [Fact]
    public void Label_encodes_colons_for_a_portable_storage_file_name() =>
        OtpLabel.Parse("example.com:rob").ToStorageFileName().ShouldBe("example.com%3Arob");

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("Lpt1")]
    [InlineData("NUL.txt")]
    [InlineData("name.")]
    [InlineData("name ")]
    public void Label_rejects_nonportable_storage_names(string value) =>
        Should.Throw<OtpValidationException>(() => OtpLabel.Parse(value));
}
