using Microsoft.FSharp.Core;
using NBomber.Contracts;

namespace GalacticDelivery.LoadTest;

public static class NBomberInterop
{
    public static bool TryGetPayload<T>(Response<T> response, out T? payload, out string error) where T : class
    {
        // NBomber exposes FSharpOption<T> as payload; keep interop here.
        var option = response.Payload;
        if (option is null || !FSharpOption<T>.get_IsSome(option))
        {
            payload = null;
            error = "empty_payload";
            return false;
        }

        payload = option.Value;
        error = string.Empty;
        return payload is not null;
    }
}
