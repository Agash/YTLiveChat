namespace YTLiveChat.Helpers
{
    internal static class Converter
    {
        public static string ToHex6Color(this int value)
        {
            return value.ToString("X")[2..].ToUpperInvariant();
        }
    }
}
