namespace RobertHodgen.Otp.Cli.Time;

public sealed class KnownSystemClock : ISystemClock
{
    private KnownSystemClock(OtpTimestamp now)
    {
        Now = now;
    }

    public OtpTimestamp Now { get; }

    public static KnownSystemClock FromTime(DateTime value) =>
        new(OtpTimestamp.FromTime(value));
}
