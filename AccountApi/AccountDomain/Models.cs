namespace AccountApi.AccountDomain;

public sealed class CurrentAccount
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public long Number { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool Active { get; set; } = true;
    public string PasswordHash { get; init; } = string.Empty;
    public string Salt { get; init; } = string.Empty;
}

public sealed class Movement
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string AccountId { get; init; } = string.Empty;
    public DateTime Date { get; init; } = DateTime.UtcNow;
    public string Type { get; init; } = "C"; // C or D
    public decimal Value { get; init; }
}

public sealed class IdempotencyRecord
{
    public string Key { get; init; } = string.Empty;
    public string? Request { get; init; }
    public string? Result { get; init; }
}


