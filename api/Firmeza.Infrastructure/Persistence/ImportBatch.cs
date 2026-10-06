namespace Firmeza.Infrastructure.Persistence;

public sealed class ImportBatch
{
    public string Hash { get; set; } = "";
    public int AppliedRows
    {
        get; set;
    }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
