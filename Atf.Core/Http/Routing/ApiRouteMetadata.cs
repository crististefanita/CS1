namespace Atf.Core.Http.Routing;

/// <summary>
/// Authentication mechanism declared by an API route contract.
/// This is route metadata, not a secret and not the runtime token itself.
/// </summary>
public enum ApiAuthType
{
    None,
    Bearer,
    ApiKey,
    Basic,
    Custom
}

/// <summary>
/// Whether a route may be called anonymously or requires its configured profile.
/// </summary>
public enum ApiAuthRequirement
{
    Optional,
    Required
}

/// <summary>
/// Typed authentication contract attached to a route.
/// The profile remains a string because profiles are intentionally configuration-driven.
/// </summary>
public sealed record ApiRouteAuthentication
{
    public ApiRouteAuthentication(
        ApiAuthType type,
        ApiAuthRequirement requirement,
        string profile)
    {
        if (string.IsNullOrWhiteSpace(profile))
        {
            throw new ArgumentException("An authentication profile is required for a route.", nameof(profile));
        }

        if (type == ApiAuthType.None && requirement == ApiAuthRequirement.Required)
        {
            throw new ArgumentException("An authentication mechanism is required when route access is required.", nameof(type));
        }

        Type = type;
        Requirement = requirement;
        Profile = profile;
    }

    public ApiAuthType Type { get; }
    public ApiAuthRequirement Requirement { get; }
    public string Profile { get; }

    public bool IsAnonymousAllowed => Requirement == ApiAuthRequirement.Optional;

    /// <summary>Stable, human-readable policy value used by the Extent report.</summary>
    public string Policy => Requirement == ApiAuthRequirement.Required
        ? $"{DisplayType(Type)} required"
        : Type == ApiAuthType.None
            ? "anonymous allowed"
            : $"{DisplayType(Type)} optional";

    public string TypeName => DisplayType(Type);

    public static ApiRouteAuthentication Anonymous(string profile = "anonymous") =>
        new(ApiAuthType.None, ApiAuthRequirement.Optional, profile);

    public static ApiRouteAuthentication Required(ApiAuthType type, string profile) =>
        new(type, ApiAuthRequirement.Required, profile);

    public static ApiRouteAuthentication Optional(ApiAuthType type, string profile) =>
        new(type, ApiAuthRequirement.Optional, profile);

    private static string DisplayType(ApiAuthType type) =>
        type switch
        {
            ApiAuthType.None => "none",
            ApiAuthType.Bearer => "bearer",
            ApiAuthType.ApiKey => "api-key",
            ApiAuthType.Basic => "basic",
            ApiAuthType.Custom => "custom",
            _ => type.ToString().ToLowerInvariant()
        };
}
