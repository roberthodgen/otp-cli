namespace RobertHodgen.Otp.Cli.Time;

public interface ISystemClock
{
    OtpTimestamp Now { get; }
}
