namespace RobertHodgen.Otp.Cli.Storage;

using Otp;

public interface IOtpRepository
{
    IReadOnlyList<OtpLabel> List();

    string? Get(OtpLabel label);

    bool CreateIfAbsent(OtpLabel label, string canonicalUri);

    void DeleteIfPresent(OtpLabel label);
}
