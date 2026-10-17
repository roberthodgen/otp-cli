namespace RobertHodgen.Otp.Cli.Time;

public sealed class RunningSystemClock : ISystemClock
{
    public OtpTimestamp Now => OtpTimestamp.FromTime(DateTime.UtcNow);
}
