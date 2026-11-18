namespace SharedModels;

public class TcpMessage
{
    public string Action { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;     // חובה בשביל ה-Login
    public string MessageText { get; set; } = string.Empty;
    public bool Success { get; set; }                        // חובה בשביל לבדוק אם הצלחנו
}