using System.Diagnostics;

namespace RobertHodgen.Otp.Cli.Tests.EndToEnd;

public sealed class CliProcess
{
    private readonly string _overrideDirectory;

    private CliProcess(string overrideDirectory)
    {
        _overrideDirectory = overrideDirectory;
    }

    public static CliProcess WithOverrideDirectory(string path) => new(path);

    public CliProcessResult Invoke(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(typeof(CliApplication).Assembly.Location);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["OTP_CLI_OVERRIDE_DIR"] = _overrideDirectory;
        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new CliProcessResult(process.ExitCode, output, error);
    }
}

public sealed record CliProcessResult(int ExitCode, string StandardOutput, string StandardError);
