namespace Common;

public class Message
{
    public string SenderName { get; set; }
    public string RecipientName { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public int RecipientId { get; set; }
    public int SenderId { get; set; }
}