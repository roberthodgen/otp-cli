namespace RobertHodgen.Otp.Cli;

public sealed class OtpLabel
{
    private static readonly HashSet<string> ReservedDeviceNames =
    [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    ];

    private OtpLabel(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static OtpLabel Parse(string value)
    {
        if (string.IsNullOrEmpty(value) ||
            value is "." or ".." ||
            value.EndsWith(' ') ||
            value.EndsWith('.') ||
            ReservedDeviceNames.Contains(Path.GetFileNameWithoutExtension(value)))
        {
            throw new OtpValidationException("The OTP label is not valid for storage.");
        }

        return new OtpLabel(value);
    }

    public static OtpLabel FromStorageFileName(string fileName) =>
        Parse(Uri.UnescapeDataString(fileName));

    public string ToStorageFileName() => Uri.EscapeDataString(Value);
}
