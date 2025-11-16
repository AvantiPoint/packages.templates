namespace NuGetFeedTemplate.Models;

public class TokenExpiration
{
    public string Description { get; set; }
    public string Expires { get; set; }
    public int DaysRemaining { get; set; }
}
