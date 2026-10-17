namespace RobertHodgen.Otp.Cli;

public sealed class RunningSystemClock : ISystemClock
{
    public OtpTimestamp Now => OtpTimestamp.FromTime(DateTime.UtcNow);
}
