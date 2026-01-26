namespace GalacticDelivery.Infrastructure;

internal static class StringTools
{
    public static Guid? MaybeGuid(string? text)
    {
        return Guid.TryParse(text, out var guid) ? guid : null;
    }
}