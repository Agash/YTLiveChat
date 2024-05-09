namespace YTLiveChat.Models.Response;

internal class Accessibility
{
    public required AccessibilityDataObj AccessibilityData { get; set; }
    public class AccessibilityDataObj
    {
        public string Label { get; set; } = string.Empty;
    }
}
