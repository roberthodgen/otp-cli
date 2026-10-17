using System.Text;

namespace RobertHodgen.Otp.Cli;

public sealed class FileOtpRepository : IOtpRepository
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly string _rootDirectory;

    public FileOtpRepository(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
    }

    public static FileOtpRepository FromEnvironment()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable("OTP_CLI_OVERRIDE_DIR");
        var rootDirectory = !string.IsNullOrEmpty(overrideDirectory)
            ? overrideDirectory
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "otp");

        return new FileOtpRepository(rootDirectory);
    }

    public IReadOnlyList<OtpLabel> List()
    {
        if (!Directory.Exists(_rootDirectory))
        {
            return [];
        }

        return Directory.EnumerateFiles(_rootDirectory)
            .Select(Path.GetFileName)
            .Where(fileName => fileName is not null)
            .Select(fileName => OtpLabel.FromStorageFileName(fileName!))
            .OrderBy(label => label.Value, StringComparer.Ordinal)
            .ToArray();
    }

    public string? Get(OtpLabel label)
    {
        var path = GetPath(label);
        return File.Exists(path) ? File.ReadAllText(path, Utf8) : null;
    }

    public bool CreateIfAbsent(OtpLabel label, string canonicalUri)
    {
        Directory.CreateDirectory(_rootDirectory);
        var path = GetPath(label);

        try
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, Utf8);
            writer.Write(canonicalUri);
            return true;
        }
        catch (IOException) when (File.Exists(path))
        {
            return false;
        }
    }

    public void DeleteIfPresent(OtpLabel label)
    {
        if (!Directory.Exists(_rootDirectory))
        {
            return;
        }

        File.Delete(GetPath(label));
    }

    private string GetPath(OtpLabel label) =>
        Path.Combine(_rootDirectory, label.ToStorageFileName());
}
