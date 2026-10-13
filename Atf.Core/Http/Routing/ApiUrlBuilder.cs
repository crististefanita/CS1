using Atf.Core.Configuration;

namespace Atf.Core.Http.Routing;

public interface IApiUrlBuilder
{
    string Build(ApiRequest request);
}

public sealed class ApiUrlBuilder : IApiUrlBuilder
{
    private readonly string _baseUrl;

    public ApiUrlBuilder(ApiSettings settings)
    {
        _baseUrl = (settings?.ToRuntimeSnapshot() ?? throw new ArgumentNullException(nameof(settings))).BaseUrl;
    }

    internal ApiUrlBuilder(ApiSettingsSnapshot settings)
    {
        _baseUrl = settings?.BaseUrl ?? throw new ArgumentNullException(nameof(settings));
    }

    public string Build(ApiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var baseUri = _baseUrl.TrimEnd('/') + "/";
        var relative = request.Path.TrimStart('/');
        var builder = new UriBuilder(new Uri(new Uri(baseUri), relative));
        if (request.Query is { Count: > 0 })
        {
            builder.Query = string.Join("&", request.Query
                .Where(item => item.Value is not null)
                .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}"));
        }

        return builder.Uri.AbsoluteUri;
    }
}
