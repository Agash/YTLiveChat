namespace YTLiveChat.Contracts.Models
{
    /// <summary>
    /// Represents a Superchat
    /// </summary>
    public class Superchat
    {
        /// <summary>
        /// Amount of $ gifted
        /// </summary>
        public required string Amount { get; set; }

        /// <summary>
        /// Color of Superchat
        /// </summary>
        public required string Color { get; set; }

        /// <summary>
        /// If Superchat is a sticker, contains an ImagePart with said Sticker
        /// </summary>
        public ImagePart? Sticker { get; set; }
    }
}
