namespace RobertHodgen.Otp.Cli.Application;

public sealed record OtpCommandOutcome(int ExitCode, string? StandardOutput, string? StandardError)
{
    public static OtpCommandOutcome Success(string? output = null) =>
        new(CliExitCodes.Success, output, null);

    public static OtpCommandOutcome Failure(string error) =>
        new(CliExitCodes.Failure, null, error + Environment.NewLine);
}
