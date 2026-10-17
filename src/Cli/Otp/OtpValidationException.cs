namespace RobertHodgen.Otp.Cli.Otp;

public sealed class OtpValidationException : Exception
{
    public OtpValidationException(string message)
        : base(message)
    {
    }
}
