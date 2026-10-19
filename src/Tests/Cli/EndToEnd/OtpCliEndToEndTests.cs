namespace RobertHodgen.Otp.Cli.Tests.EndToEnd;

public sealed class OtpCliEndToEndTests
{
    [Fact]
    public void List_with_a_missing_override_directory_reports_an_empty_store()
    {
        using var root = new TemporaryDirectory(createImmediately: false);

        var result = CliProcess.WithOverrideDirectory(root.Path).Invoke("list");

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldBe("total 0" + Environment.NewLine);
        result.StandardError.ShouldBeEmpty();
    }

    [Fact]
    public void Create_list_read_and_delete_use_the_override_directory()
    {
        using var root = new TemporaryDirectory();
        var cli = CliProcess.WithOverrideDirectory(root.Path);

        cli.Invoke("create", "otpauth://totp/example.com?secret=JBSWY3DPFQQHO33SNRSCC").ExitCode.ShouldBe(0);
        cli.Invoke("list").StandardOutput.ShouldBe("total 1" + Environment.NewLine + "example.com" + Environment.NewLine);
        cli.Invoke("read", "example.com").StandardOutput.ShouldMatch("^[0-9]{6}\\r?\\n$");
        cli.Invoke("delete", "example.com").ExitCode.ShouldBe(0);
        cli.Invoke("list").StandardOutput.ShouldBe("total 0" + Environment.NewLine);
    }

    [Fact]
    public void Seeded_friendly_label_is_listed_read_and_deleted_without_manual_encoding()
    {
        using var root = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(root.Path, "example.com%3Arob"),
            "otpauth://totp/example.com%3Arob?secret=JBSWY3DPFQQHO33SNRSCC");
        var cli = CliProcess.WithOverrideDirectory(root.Path);

        cli.Invoke("list").StandardOutput.ShouldBe("total 1" + Environment.NewLine + "example.com:rob" + Environment.NewLine);
        cli.Invoke("read", "example.com:rob").StandardOutput.ShouldMatch("^[0-9]{6}\\r?\\n$");
        cli.Invoke("delete", "example.com:rob").ExitCode.ShouldBe(0);

        File.Exists(Path.Combine(root.Path, "example.com%3Arob")).ShouldBeFalse();
    }

    [Fact]
    public void Read_missing_label_returns_the_standard_error()
    {
        using var root = new TemporaryDirectory();

        var result = CliProcess.WithOverrideDirectory(root.Path).Invoke("read", "missing");

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldBe("no OTP with that label was found" + Environment.NewLine);
    }
}
