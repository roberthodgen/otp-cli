namespace RobertHodgen.Otp.Cli;

public sealed class TotpCredential
{
    private TotpCredential(OtpLabel label, string secret, string algorithm, int digits, int period, string? issuer)
    {
        Label = label;
        Secret = secret;
        Algorithm = algorithm;
        Digits = digits;
        Period = period;
        Issuer = issuer;
        CanonicalUri = BuildCanonicalUri();
    }

    public OtpLabel Label { get; }

    public string CanonicalUri { get; }

    internal string Secret { get; }

    internal string Algorithm { get; }

    internal int Digits { get; }

    internal int Period { get; }

    internal string? Issuer { get; }

    public static TotpCredential Create(OtpLabel label, string secret, int? period, string? algorithm, string? issuer) =>
        new(label, NormalizeSecret(secret), NormalizeAlgorithm(algorithm ?? "SHA1"), 6, NormalizePeriod(period ?? 30), issuer);

    public static TotpCredential Parse(string uriText)
    {
        if (!Uri.TryCreate(uriText, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals("otpauth", StringComparison.OrdinalIgnoreCase) ||
            !uri.Host.Equals("totp", StringComparison.OrdinalIgnoreCase))
        {
            throw new OtpValidationException("The OTP URI is not a supported TOTP URI.");
        }

        var label = ParseLabel(uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped));
        var parameters = ParseParameters(uri.Query);

        if (!parameters.TryGetValue("secret", out var secret))
        {
            throw new OtpValidationException("The OTP URI does not include a secret.");
        }

        var algorithm = NormalizeAlgorithm(parameters.GetValueOrDefault("algorithm") ?? "SHA1");
        var digits = NormalizeDigits(parameters.GetValueOrDefault("digits") ?? "6");
        var period = NormalizePeriod(parameters.GetValueOrDefault("period") ?? "30");
        return new TotpCredential(label, NormalizeSecret(secret), algorithm, digits, period, parameters.GetValueOrDefault("issuer"));
    }

    private static OtpLabel ParseLabel(string rawLabel)
    {
        if (string.IsNullOrEmpty(rawLabel))
        {
            throw new OtpValidationException("The OTP URI does not include a label.");
        }

        var separator = FindIssuerSeparator(rawLabel);
        if (separator.Index < 0)
        {
            return OtpLabel.Parse(Uri.UnescapeDataString(rawLabel));
        }

        var issuer = Uri.UnescapeDataString(rawLabel[..separator.Index]);
        var account = Uri.UnescapeDataString(rawLabel[(separator.Index + separator.Length)..]).TrimStart();
        if (issuer.Contains(':') || account.Contains(':'))
        {
            throw new OtpValidationException("The OTP URI label is not valid.");
        }

        return OtpLabel.Parse($"{issuer}: {account}");
    }

    private static (int Index, int Length) FindIssuerSeparator(string label)
    {
        for (var index = 0; index < label.Length; index++)
        {
            if (label[index] == ':')
            {
                return (index, 1);
            }

            if (index + 2 < label.Length && label[index] == '%' && label[index + 1] == '3' &&
                (label[index + 2] is 'A' or 'a'))
            {
                return (index, 3);
            }
        }

        return (-1, 0);
    }

    private static Dictionary<string, string> ParseParameters(string query)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = item.IndexOf('=');
            var name = Uri.UnescapeDataString(separator < 0 ? item : item[..separator]);
            var value = Uri.UnescapeDataString(separator < 0 ? string.Empty : item[(separator + 1)..]);
            var known = name.Equals("secret", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("algorithm", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("digits", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("period", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("issuer", StringComparison.OrdinalIgnoreCase);

            if (known && !parameters.TryAdd(name, value))
            {
                throw new OtpValidationException("The OTP URI repeats a known parameter.");
            }

            if (known)
            {
                parameters[name] = value;
            }
        }

        return parameters;
    }

    private static string NormalizeSecret(string secret)
    {
        var normalized = secret.ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized) || normalized.Any(value =>
                !((value >= 'A' && value <= 'Z') || (value >= '2' && value <= '7'))))
        {
            throw new OtpValidationException("The OTP secret is not valid Base32.");
        }

        return normalized;
    }

    private static string NormalizeAlgorithm(string algorithm)
    {
        var normalized = algorithm.ToUpperInvariant();
        return normalized is "SHA1" or "SHA256" or "SHA512"
            ? normalized
            : throw new OtpValidationException("The OTP algorithm is not supported.");
    }

    private static int NormalizeDigits(string digits) => digits switch
    {
        "6" => 6,
        "8" => 8,
        _ => throw new OtpValidationException("The OTP digit count is not supported."),
    };

    private static int NormalizePeriod(int period) => period > 0
        ? period
        : throw new OtpValidationException("The OTP period must be positive.");

    private static int NormalizePeriod(string period) =>
        int.TryParse(period, out var value) ? NormalizePeriod(value) : throw new OtpValidationException("The OTP period must be positive.");

    private string BuildCanonicalUri()
    {
        var parameters = new List<string> { $"secret={Secret}" };
        if (Algorithm != "SHA1") parameters.Add($"algorithm={Algorithm}");
        if (Digits != 6) parameters.Add($"digits={Digits}");
        if (Period != 30) parameters.Add($"period={Period}");
        if (Issuer is not null) parameters.Add($"issuer={Uri.EscapeDataString(Issuer)}");
        return $"otpauth://totp/{Uri.EscapeDataString(Label.Value)}?{string.Join("&", parameters)}";
    }
}
