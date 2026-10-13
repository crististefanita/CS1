using Atf.Core.Configuration.Models;

namespace Atf.Core.Configuration.Validation;

/// <summary>Validates route shape and uniqueness.</summary>
internal static class RouteConfigurationValidator
{
    public static void Validate(RouteSettings routes)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(RouteSettings.Users)] = routes.Users,
            [nameof(RouteSettings.Posts)] = routes.Posts,
            [nameof(RouteSettings.UserPosts)] = routes.UserPosts
        };

        foreach (var (name, path) in values)
        {
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
            {
                ConfigurationRuleViolation.Throw("ATF-ROUTE-001", $"Modules:Api:Api:Routes:{name}", "Route must be a non-empty relative path starting with '/'.");
            }
        }

        var duplicate = values.GroupBy(pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            ConfigurationRuleViolation.Throw(
                "ATF-ROUTE-002", "Modules:Api:Api:Routes",
                $"Routes contain duplicate route paths: {string.Join(", ", duplicate.Select(pair => pair.Key))}.");
        }
    }
}
