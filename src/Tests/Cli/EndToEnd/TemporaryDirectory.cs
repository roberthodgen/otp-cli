namespace RobertHodgen.Otp.Cli.Tests.EndToEnd;

public sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory(bool createImmediately = true)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "otp-cli-tests", Guid.NewGuid().ToString("N"));
        if (createImmediately)
        {
            Directory.CreateDirectory(Path);
        }
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
