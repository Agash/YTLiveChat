namespace YTLiveChat.Contracts.Models
{
    public class Superchat
    {
        public required string Amount { get; set; }
        public required string Color { get; set; }
        public ImagePart? Sticker { get; set; }
    }
}
