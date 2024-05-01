namespace YTLiveChat.Contracts.Models
{
    /// <summary>
    /// ChatItem containing the full object with any MessageParts and Author details
    /// </summary>
    public class ChatItem
    {
        /// <summary>
        /// Unique Identifier
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Author of the ChatItem
        /// </summary>
        public required Author Author { get; set; }

        /// <summary>
        /// Array of all message parts (Image, Text or Emoji variant)
        /// </summary>
        public required MessagePart[] Message { get; set; }

        /// <summary>
        /// Contains the Superchat if any was given
        /// </summary>
        public Superchat? Superchat { get; set; }

        /// <summary>
        /// Whether or not Author has a Membership on the current Live Channel
        /// </summary>
        public bool IsMembership { get; set; }

        /// <summary>
        /// Whether or not Author is Verified on YT
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Whether or not Author is Owner of the current Live Channel
        /// </summary>
        public bool IsOwner { get; set; }

        /// <summary>
        /// Whether or not Author is a Moderator of the current Live Channel
        /// </summary>
        public bool IsModerator { get; set; }

        /// <summary>
        /// Timestamp of the ChatItem creation
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
