namespace YTLiveChat.Models.Response
{
    internal class ContinuationContents
    {
        public required LiveChatContinuationObj LiveChatContinuation { get; set; }
        public class LiveChatContinuationObj
        {
            public Continuation[] Continuations { get; set; } = [];
            public YTAction[] Actions { get; set; } = [];
        }
    }



    internal class Continuation
    {
        public InvalidationContinuation? InvalidationContinuationData { get; set; }
        public TimedContinuation? TimedContinuationData { get; set; }
    }

    internal class InvalidationId
    {
        public required string ObjectId { get; set; }
        public required int ObjectSource { get; set; }
        public required string Topic { get; set; }
        public required bool SubscribeToGcmTopics { get; set; }
        public required string ProtoCreationTimestampMs { get; set; }
    }

    internal class InvalidationContinuation
    {
        public required InvalidationId InvalidationId { get; set; }
        public required int TimeoutMs { get; set; }
        public required string Continuation { get; set; }
    }

    internal class TimedContinuation
    {
        public required int TimeoutMs { get; set; }
        public required string Continuation { get; set; }
        public required string ClickTrackingParams { get; set; }
    }
}
