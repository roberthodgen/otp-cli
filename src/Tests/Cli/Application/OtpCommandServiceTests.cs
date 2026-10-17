using Shouldly;
using Xunit;

namespace RobertHodgen.Otp.Cli.Tests.Application;

public sealed class OtpCommandServiceTests
{
    [Fact]
    public void List_returns_total_zero_when_the_repository_is_empty()
    {
        var service = CreateService();

        service.List().ShouldBe(new OtpCommandOutcome(0, $"total 0{Environment.NewLine}", null));
    }

    [Fact]
    public void Read_returns_the_fixed_clock_token_without_exposing_the_uri()
    {
        var repository = new FakeRepository { ["example.com"] = RfcUri };
        var service = CreateService(repository);

        var outcome = service.Read("example.com");

        outcome.ShouldBe(new OtpCommandOutcome(0, "94287082" + Environment.NewLine, null));
    }

    [Fact]
    public void Read_missing_label_returns_the_required_safe_error()
    {
        var service = CreateService();

        service.Read("missing").ShouldBe(
            new OtpCommandOutcome(1, null, "no OTP with that label was found" + Environment.NewLine));
    }

    [Fact]
    public void Copy_passes_the_generated_token_to_the_clipboard()
    {
        var repository = new FakeRepository { ["example.com"] = RfcUri };
        var clipboard = new FakeClipboard();
        var service = CreateService(repository, clipboard);

        service.Copy("example.com").ShouldBe(new OtpCommandOutcome(0, null, null));

        clipboard.CopiedText.ShouldBe("94287082");
    }

    [Fact]
    public void Copy_missing_label_returns_the_required_safe_error()
    {
        var service = CreateService();

        service.Copy("missing").ShouldBe(
            new OtpCommandOutcome(1, null, "no OTP with that label was found" + Environment.NewLine));
    }

    [Fact]
    public void Create_from_a_plain_label_stores_a_canonical_uri_without_output()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        service.Create("rob", "JBSWY3DP", null, null, "example.com")
            .ShouldBe(new OtpCommandOutcome(0, null, null));

        repository["rob"].ShouldBe("otpauth://totp/rob?secret=JBSWY3DP&issuer=example.com");
    }

    [Fact]
    public void Corrupt_stored_credential_does_not_expose_its_secret()
    {
        var repository = new FakeRepository { ["example.com"] = "otpauth://totp/example.com?secret=invalid!" };
        var service = CreateService(repository);

        var outcome = service.Read("example.com");

        outcome.ExitCode.ShouldBe(1);
        outcome.StandardError.ShouldNotBeNull();
        outcome.StandardError.ShouldNotContain("invalid!");
        outcome.StandardError.ShouldNotContain("otpauth://");
    }

    [Fact]
    public void Delete_missing_labels_is_silent_success()
    {
        var service = CreateService();

        service.Delete(["missing"]).ShouldBe(new OtpCommandOutcome(0, null, null));
    }

    [Fact]
    public void Delete_reports_the_label_that_could_not_be_deleted()
    {
        var repository = new FakeRepository { ThrowOnDelete = "broken" };
        var service = CreateService(repository);

        var outcome = service.Delete(["good", "broken"]);

        outcome.ExitCode.ShouldBe(1);
        outcome.StandardError.ShouldNotBeNull();
        outcome.StandardError.ShouldContain("broken");
    }

    private static OtpCommandService CreateService(FakeRepository? repository = null, FakeClipboard? clipboard = null) =>
        new(repository ?? new FakeRepository(), KnownSystemClock.FromTime(DateTime.UnixEpoch.AddSeconds(59)), clipboard ?? new FakeClipboard());

    private const string RfcUri = "otpauth://totp/example.com?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&digits=8";

    private sealed class FakeClipboard : IOtpClipboard
    {
        public string? CopiedText { get; private set; }

        public void Copy(string text) => CopiedText = text;
    }

    private sealed class FakeRepository : IOtpRepository
    {
        private readonly Dictionary<string, string> _entries = new(StringComparer.Ordinal);

        public string? ThrowOnDelete { get; init; }

        public string this[string label]
        {
            get => _entries[label];
            set => _entries[label] = value;
        }

        public IReadOnlyList<OtpLabel> List() => _entries.Keys.Select(OtpLabel.Parse).ToArray();

        public string? Get(OtpLabel label) => _entries.GetValueOrDefault(label.Value);

        public bool CreateIfAbsent(OtpLabel label, string canonicalUri)
        {
            if (_entries.ContainsKey(label.Value)) return false;
            _entries.Add(label.Value, canonicalUri);
            return true;
        }

        public void DeleteIfPresent(OtpLabel label)
        {
            if (label.Value == ThrowOnDelete) throw new IOException();
            _entries.Remove(label.Value);
        }
    }
}
