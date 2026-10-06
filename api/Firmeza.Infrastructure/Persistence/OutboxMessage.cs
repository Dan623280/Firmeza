namespace Firmeza.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SaleId
    {
        get; set;
    }
    public string Status { get; set; } = "Pending";
    public int Attempts
    {
        get; set;
    }
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LeaseUntil
    {
        get; set;
    }
    public string? ReceiptKey
    {
        get; set;
    }
}
public sealed class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId
    {
        get; set;
    }
    public string Key { get; set; } = "";
    public string Hash { get; set; } = "";
    public Guid SaleId
    {
        get; set;
    }
}
