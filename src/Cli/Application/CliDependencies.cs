namespace RobertHodgen.Otp.Cli.Application;

public sealed class CliDependencies(OtpCommandService service, TextWriter output, TextWriter error)
{
    public OtpCommandService Service { get; } = service;

    public TextWriter Output { get; } = output;

    public TextWriter Error { get; } = error;
}
