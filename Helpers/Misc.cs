namespace YTLiveChat.Helpers;

internal static class Misc
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> o)
        where T : class => o.Where(x => x != null)!;
}
