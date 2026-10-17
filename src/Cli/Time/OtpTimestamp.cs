namespace RobertHodgen.Otp.Cli;

public readonly record struct OtpTimestamp(DateTime Value)
{
    public static OtpTimestamp FromTime(DateTime value) => new(value.ToUniversalTime());
}
