using System.CommandLine;

namespace RobertHodgen.Otp.Cli;

public static class CliApplication
{
    public static RootCommand CreateRootCommand(TextWriter? error = null)
    {
        _ = error ?? Console.Error;

        return new RootCommand();
    }
}
