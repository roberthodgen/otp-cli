namespace RobertHodgen.Otp.Cli;

public interface IOtpRepository
{
    IReadOnlyList<OtpLabel> List();

    string? Get(OtpLabel label);

    bool CreateIfAbsent(OtpLabel label, string canonicalUri);

    void DeleteIfPresent(OtpLabel label);
}
