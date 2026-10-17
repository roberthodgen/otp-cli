namespace RobertHodgen.Otp.Cli.Application;

public sealed record OtpCommandOutcome(int ExitCode, string? StandardOutput, string? StandardError);
