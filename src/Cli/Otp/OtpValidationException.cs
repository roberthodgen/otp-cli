namespace RobertHodgen.Otp.Cli;

public sealed class OtpValidationException : Exception
{
    public OtpValidationException(string message)
        : base(message)
    {
    }
}
