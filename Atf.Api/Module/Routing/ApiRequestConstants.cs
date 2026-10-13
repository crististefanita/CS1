using System.Net.Http;

namespace Atf.Common.Routing;

/// <summary>
/// Request-level constants owned by the API route catalog.
///
/// Keeping these values beside the route catalog makes the supported request
/// vocabulary discoverable from one place, while paths remain configurable
/// through RouteSettings and authentication remains typed metadata on ApiRoute.
/// HttpMethod is a reference type, therefore these are readonly values rather
/// than C# const fields.
/// </summary>
public static class ApiRequestConstants
{
    public static readonly HttpMethod Get = HttpMethod.Get;
    public static readonly HttpMethod Post = HttpMethod.Post;
    public static readonly HttpMethod Put = HttpMethod.Put;
    public static readonly HttpMethod Patch = HttpMethod.Patch;
    public static readonly HttpMethod Delete = HttpMethod.Delete;
}
