namespace YTLiveChat.Helpers;

internal static class Converter
{
    /// <summary>
    /// Converts a long representing an ARGB color value (like those from YouTube API)
    /// into a 6-digit uppercase hex color string (RGB only, ignoring alpha).
    /// Handles potential negative values correctly.
    /// </summary>
    /// <param name="value">The ARGB color value as a long.</param>
    /// <returns>A 6-digit hex color string (e.g., "FF0000" for red), or null if input is invalid.</returns>
    public static string? ToHex6Color(this long value)
    {
        try
        {
            // YouTube uses signed int32 for ARGB stored in a long.
            // Casting to uint handles negative representation and allows bitwise operations.
            uint argb = (uint)value;
            uint rgb = argb & 0xFFFFFF; // Mask out the alpha channel (top byte)

            // Format the remaining RGB value as a 6-digit hex string, padding with leading zeros.
            return rgb.ToString("X6").ToUpperInvariant();
        }
        catch (OverflowException)
        {
            // Handle cases where the long value doesn't fit into uint, though unlikely for ARGB
            return null;
        }
    }
}
