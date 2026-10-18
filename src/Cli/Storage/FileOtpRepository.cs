using System.Text;

namespace RobertHodgen.Otp.Cli.Storage;

using Otp;

public sealed class FileOtpRepository(string rootDirectory) : IOtpRepository
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

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
        if (!Directory.Exists(rootDirectory))
        {
            return [];
        }

        return Directory.EnumerateFiles(rootDirectory)
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
        Directory.CreateDirectory(rootDirectory);
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
        if (!Directory.Exists(rootDirectory))
        {
            return;
        }

        File.Delete(GetPath(label));
    }

    private string GetPath(OtpLabel label) =>
        Path.Combine(rootDirectory, label.ToStorageFileName());
}
