namespace Firmeza.Infrastructure.Identity;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "Firmeza";
    public string Audience { get; set; } = "Firmeza.Client";
    public string SigningKey { get; set; } = "";
    public int ExpirationMinutes { get; set; } = 30;
}
