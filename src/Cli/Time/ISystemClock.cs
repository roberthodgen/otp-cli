namespace RobertHodgen.Otp.Cli;

public interface ISystemClock
{
    OtpTimestamp Now { get; }
}
