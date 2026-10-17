namespace RobertHodgen.Otp.Cli;

public sealed class CliDependencies
{
    public CliDependencies(OtpCommandService service, TextWriter output, TextWriter error)
    {
        Service = service;
        Output = output;
        Error = error;
    }

    public OtpCommandService Service { get; }

    public TextWriter Output { get; }

    public TextWriter Error { get; }
}
