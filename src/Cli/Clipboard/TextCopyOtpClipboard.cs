namespace RobertHodgen.Otp.Cli.Clipboard;

using TextCopy;

public sealed class TextCopyOtpClipboard : IOtpClipboard
{
    public void Copy(string text) => ClipboardService.SetText(text);
}
