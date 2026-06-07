namespace Spur.Extensions.Extras.PasswordGen;

public sealed class PasswordGenSettings
{
    public int  DefaultLength    { get; set; } = 16;
    public bool IncludeSymbols   { get; set; } = true;
    public bool IncludeNumbers   { get; set; } = true;
    public bool IncludeUppercase { get; set; } = true;
}
