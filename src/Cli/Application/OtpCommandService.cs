namespace RobertHodgen.Otp.Cli.Application;

using Clipboard;
using Otp;
using Storage;
using Time;

public sealed class OtpCommandService(IOtpRepository repository, ISystemClock clock, IOtpClipboard clipboard)
{
    private const string MissingLabelError = "no OTP with that label was found";

    public OtpCommandOutcome List()
    {
        try
        {
            var labels = repository.List();
            var output = $"total {labels.Count}{Environment.NewLine}" +
                         string.Concat(labels.Select(label => label.Value + Environment.NewLine));
            return OtpCommandOutcome.Success(output);
        }
        catch (Exception)
        {
            return OtpCommandOutcome.Failure("unable to list OTPs");
        }
    }

    public OtpCommandOutcome Create(string labelOrUrl, string? secret, int? period, string? algorithm, string? issuer)
    {
        try
        {
            var isUri = labelOrUrl.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase);
            if (isUri && (secret is not null || period is not null || algorithm is not null || issuer is not null))
            {
                return OtpCommandOutcome.Failure("OTP URI creation options cannot be combined");
            }

            if (!isUri && secret is null)
            {
                return OtpCommandOutcome.Failure("a secret is required when creating an OTP from a label");
            }

            var credential = isUri
                ? TotpCredential.Parse(labelOrUrl)
                : TotpCredential.Create(OtpLabel.Parse(labelOrUrl), secret!, period, algorithm, issuer);
            return repository.CreateIfAbsent(credential.Label, credential.CanonicalUri)
                ? OtpCommandOutcome.Success()
                : OtpCommandOutcome.Failure("an OTP with that label already exists");
        }
        catch (Exception)
        {
            return OtpCommandOutcome.Failure("unable to create OTP");
        }
    }

    public OtpCommandOutcome Read(string label)
    {
        try
        {
            var credential = GetCredential(label);
            return credential is null
                ? OtpCommandOutcome.Failure(MissingLabelError)
                : OtpCommandOutcome.Success(TotpGenerator.Generate(credential, clock.Now) + Environment.NewLine);
        }
        catch (Exception)
        {
            return OtpCommandOutcome.Failure(MissingLabelError);
        }
    }

    public OtpCommandOutcome Copy(string label)
    {
        try
        {
            var credential = GetCredential(label);
            if (credential is null)
            {
                return OtpCommandOutcome.Failure(MissingLabelError);
            }

            clipboard.Copy(TotpGenerator.Generate(credential, clock.Now));
            return OtpCommandOutcome.Success();
        }
        catch (Exception)
        {
            return OtpCommandOutcome.Failure("unable to copy OTP");
        }
    }

    public OtpCommandOutcome Delete(IReadOnlyList<string> labels)
    {
        string? currentLabel = null;
        try
        {
            foreach (var label in labels)
            {
                currentLabel = label;
                repository.DeleteIfPresent(OtpLabel.Parse(label));
            }

            return OtpCommandOutcome.Success();
        }
        catch (Exception)
        {
            return OtpCommandOutcome.Failure($"unable to delete OTP: {currentLabel}");
        }
    }

    private TotpCredential? GetCredential(string label)
    {
        var uri = repository.Get(OtpLabel.Parse(label));
        return uri is null ? null : TotpCredential.Parse(uri);
    }
}
