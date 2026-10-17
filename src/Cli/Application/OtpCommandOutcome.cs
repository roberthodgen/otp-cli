namespace RobertHodgen.Otp.Cli;

public sealed record OtpCommandOutcome(int ExitCode, string? StandardOutput, string? StandardError);
