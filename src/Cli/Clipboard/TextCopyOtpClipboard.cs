using TextCopy;

namespace RobertHodgen.Otp.Cli;

public sealed class TextCopyOtpClipboard : IOtpClipboard
{
    public void Copy(string text) => ClipboardService.SetText(text);
}
