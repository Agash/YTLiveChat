namespace YTLiveChat.Tests.TestData;

internal static class WebSnapshotTestData
{
    public static string LoadWebSnapshot(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "WebSnapshots", fileName);
        return File.ReadAllText(path);
    }

    // Captured from live pages on 2026-02-17.
    public static string HakosLivePageSnapshot() =>
        """
        <!DOCTYPE html><html><head>
        <link rel="canonical" href="https://www.youtube.com/watch?v=oPOBYMu2zk8">
        <script>
        window.ytcfg.set({
          "INNERTUBE_API_KEY":"AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8",
          "INNERTUBE_CONTEXT_CLIENT_VERSION":"2.20260213.01.00",
          "continuation":"0ofMyAOAARpeQ2lrcUp3b1lWVU5uYlZCdWVDMUZSV1ZQY2xwVFp6VlVhWGMzV2xKUkVndHZVRTlDV1UxMU1ucHJPQm",
          "isLiveNow":false,
          "isUpcoming":true
        });
        var ytInitialPlayerResponse = {
          "microformat": {
            "playerMicroformatRenderer": {
              "liveBroadcastDetails": {
                "isLiveNow": false,
                "startTimestamp": "2026-02-17T10:00:00+00:00"
              }
            }
          },
          "videoDetails": {
            "videoId": "oPOBYMu2zk8",
            "isLiveContent": true,
            "isUpcoming": true
          }
        };
        </script></head><body></body></html>
        """;

    // Captured from live pages on 2026-02-17.
    public static string BijouLivePageSnapshot() =>
        """
        <!DOCTYPE html><html><head>
        <link rel="canonical" href="https://www.youtube.com/watch?v=17PFTNoO_RE">
        <script>
        window.ytcfg.set({
          "INNERTUBE_API_KEY":"AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8",
          "INNERTUBE_CONTEXT_CLIENT_VERSION":"2.20260213.01.00",
          "continuation":"0ofMyAOAARpeQ2lrcUp3b1lWVU01Y0Y5c2NWRXdSa1ZFZWpNeU4xWm5aalZLZDNGQkVnc3hOMUJHVkU1dlQxOVNSUm",
          "isLiveNow":true
        });
        var ytInitialPlayerResponse = {
          "microformat": {
            "playerMicroformatRenderer": {
              "liveBroadcastDetails": {
                "isLiveNow": true,
                "startTimestamp": "2026-02-17T07:00:03+00:00"
              }
            }
          },
          "videoDetails": {
            "videoId": "17PFTNoO_RE",
            "isLive": true,
            "isLiveContent": true
          }
        };
        </script></head><body></body></html>
        """;

    // Snapshot fragments captured from /@HakosBaelz/streams and /@KosekiBijou/streams on 2026-02-17.
    public static string StreamsPageSnapshotFragments() =>
        """
        <html><body><script>
        {"videoRenderer":{"videoId":"197OEpjj8RI","viewCountText":{"runs":[{"text":"3"},{"text":" waiting"}]},"upcomingEventData":{"startTime":"1819720800"},"shortViewCountText":{"runs":[{"text":"3"},{"text":" waiting"}]},"thumbnailOverlays":[{"thumbnailOverlayTimeStatusRenderer":{"text":{"simpleText":"Upcoming"},"style":"UPCOMING"}}]}}
        {"videoRenderer":{"videoId":"oPOBYMu2zk8","viewCountText":{"runs":[{"text":"787"},{"text":" waiting"}]},"upcomingEventData":{"startTime":"1771322400"},"shortViewCountText":{"runs":[{"text":"787"},{"text":" waiting"}]},"thumbnailOverlays":[{"thumbnailOverlayTimeStatusRenderer":{"text":{"simpleText":"Upcoming"},"style":"UPCOMING"}}]}}
        {"videoRenderer":{"videoId":"17PFTNoO_RE","viewCountText":{"runs":[{"text":"14k"},{"text":" watching"}]},"shortViewCountText":{"runs":[{"text":"14k"},{"text":" watching"}]},"thumbnailOverlays":[{"thumbnailOverlayTimeStatusRenderer":{"text":{"runs":[{"text":"LIVE"}]},"style":"LIVE"}}]}}
        {"videoRenderer":{"videoId":"hlDFczhR2mo","viewCountText":{"runs":[{"text":"8"},{"text":" waiting"}]},"upcomingEventData":{"startTime":"1788748200"},"shortViewCountText":{"runs":[{"text":"8"},{"text":" waiting"}]},"thumbnailOverlays":[{"thumbnailOverlayTimeStatusRenderer":{"text":{"simpleText":"Upcoming"},"style":"UPCOMING"}}]}}
        </script></body></html>
        """;

    public static string HakosStreamsSnapshotFragments() =>
        """
        <html><body><script>
        {"videoRenderer":{"videoId":"197OEpjj8RI","viewCountText":{"runs":[{"text":"3"},{"text":" waiting"}]},"upcomingEventData":{"startTime":"1819720800"},"shortViewCountText":{"runs":[{"text":"3"},{"text":" waiting"}]},"thumbnailOverlays":[{"thumbnailOverlayTimeStatusRenderer":{"text":{"simpleText":"Upcoming"},"style":"UPCOMING"}}]}}
        {"videoRenderer":{"videoId":"oPOBYMu2zk8","viewCountText":{"runs":[{"text":"787"},{"text":" waiting"}]},"upcomingEventData":{"startTime":"1771322400"},"shortViewCountText":{"runs":[{"text":"787"},{"text":" waiting"}]},"thumbnailOverlays":[{"thumbnailOverlayTimeStatusRenderer":{"text":{"simpleText":"Upcoming"},"style":"UPCOMING"}}]}}
        </script></body></html>
        """;
}
