namespace Common;

public record TokenResult
{
    public string? Token { get; set; }
    public DateTime ExpirationDate { get; set; }
}