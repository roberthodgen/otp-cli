namespace RobertHodgen.Otp.Cli;

public sealed class OtpCommandService
{
    private const string MissingLabelError = "no OTP with that label was found";
    private readonly IOtpRepository _repository;
    private readonly ISystemClock _clock;
    private readonly IOtpClipboard _clipboard;

    public OtpCommandService(IOtpRepository repository, ISystemClock clock, IOtpClipboard clipboard)
    {
        _repository = repository;
        _clock = clock;
        _clipboard = clipboard;
    }

    public OtpCommandOutcome List()
    {
        try
        {
            var labels = _repository.List();
            var output = $"total {labels.Count}{Environment.NewLine}" +
                         string.Concat(labels.Select(label => label.Value + Environment.NewLine));
            return Success(output);
        }
        catch (Exception)
        {
            return Failure("unable to list OTPs");
        }
    }

    public OtpCommandOutcome Create(string labelOrUrl, string? secret, int? period, string? algorithm, string? issuer)
    {
        try
        {
            var isUri = labelOrUrl.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase);
            if (isUri && (secret is not null || period is not null || algorithm is not null || issuer is not null))
            {
                return Failure("OTP URI creation options cannot be combined");
            }

            if (!isUri && secret is null)
            {
                return Failure("a secret is required when creating an OTP from a label");
            }

            var credential = isUri
                ? TotpCredential.Parse(labelOrUrl)
                : TotpCredential.Create(OtpLabel.Parse(labelOrUrl), secret!, period, algorithm, issuer);
            return _repository.CreateIfAbsent(credential.Label, credential.CanonicalUri)
                ? Success()
                : Failure("an OTP with that label already exists");
        }
        catch (Exception)
        {
            return Failure("unable to create OTP");
        }
    }

    public OtpCommandOutcome Read(string label)
    {
        try
        {
            var credential = GetCredential(label);
            return credential is null
                ? Failure(MissingLabelError)
                : Success(TotpGenerator.Generate(credential, _clock.Now) + Environment.NewLine);
        }
        catch (Exception)
        {
            return Failure(MissingLabelError);
        }
    }

    public OtpCommandOutcome Copy(string label)
    {
        try
        {
            var credential = GetCredential(label);
            if (credential is null)
            {
                return Failure(MissingLabelError);
            }

            _clipboard.Copy(TotpGenerator.Generate(credential, _clock.Now));
            return Success();
        }
        catch (Exception)
        {
            return Failure("unable to copy OTP");
        }
    }

    public OtpCommandOutcome Delete(IReadOnlyList<string> labels)
    {
        try
        {
            foreach (var label in labels)
            {
                _repository.DeleteIfPresent(OtpLabel.Parse(label));
            }

            return Success();
        }
        catch (Exception)
        {
            return Failure("unable to delete OTP");
        }
    }

    private TotpCredential? GetCredential(string label)
    {
        var uri = _repository.Get(OtpLabel.Parse(label));
        return uri is null ? null : TotpCredential.Parse(uri);
    }

    private static OtpCommandOutcome Success(string? output = null) =>
        new(CliExitCodes.Success, output, null);

    private static OtpCommandOutcome Failure(string error) =>
        new(CliExitCodes.Failure, null, error + Environment.NewLine);
}
