namespace RobertHodgen.Otp.Cli.Tests.Time;

using Cli.Time;

public sealed class ClockTests
{
    [Fact]
    public void FromTime_normalizes_values_to_utc()
    {
        var timestamp = OtpTimestamp.FromTime(
            new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc));

        timestamp.Value.Kind.ShouldBe(DateTimeKind.Utc);
        timestamp.Value.ShouldBe(new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Known_clock_returns_its_fixed_timestamp()
    {
        var expected = OtpTimestamp.FromTime(DateTime.UnixEpoch.AddSeconds(59));

        KnownSystemClock.FromTime(expected.Value).Now.ShouldBe(expected);
    }
}
