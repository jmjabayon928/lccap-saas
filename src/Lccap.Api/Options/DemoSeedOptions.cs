namespace Lccap.Api.Options;

public sealed class DemoSeedOptions
{
    public bool Enabled { get; set; }
    public string Password { get; set; } = string.Empty;

    public bool IsValid()
    {
        return !Enabled || !string.IsNullOrWhiteSpace(Password);
    }
}
