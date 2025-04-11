// ytlivechat-library.xml -> Helpers/Converter.cs

namespace YTLiveChat.Helpers;

internal static class Converter
{
    /// <summary>
    /// Converts a long representing an ARGB color value (like those from YouTube API)
    /// into a 6-digit uppercase hex color string (RGB only, ignoring alpha).
    /// </summary>
    /// <param name="value">The ARGB color value as a long.</param>
    /// <returns>A 6-digit hex color string (e.g., "FF0000" for red).</returns>
    public static string ToHex6Color(this long value)
    {
        // YouTube uses signed int32 for ARGB (alpha in high byte).
        // Casting the long to uint effectively handles the potential negative values
        // correctly for bitwise operations, assuming it represents an ARGB value.
        // We mask with 0xFFFFFF to ignore the alpha byte and keep only RGB.
        uint argb = (uint)value;
        uint rgb = argb & 0xFFFFFF; // Mask out the alpha channel

        // Format the remaining RGB value as a 6-digit hex string, padding with leading zeros if necessary.
        return rgb.ToString("X6").ToUpperInvariant();
    }
}