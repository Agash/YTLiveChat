namespace YTLiveChat.Contracts.Models
{
    public class ChatItem
    {
        public required string Id { get; set; }
        public required Author Author { get; set; }
        public required MessagePart[] Message { get; set; }
        public Superchat? Superchat { get; set; }
        public bool IsMembership { get; set; }
        public bool IsVerified { get; set; }
        public bool IsOwner { get; set; }
        public bool IsModerator { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
