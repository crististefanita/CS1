using Atf.Core.Configuration;
using Atf.Core.Configuration.Security;

namespace Atf.Core.Tests.Configuration;

public sealed class ApiSettingsSnapshotTests
{
    [Fact(DisplayName = "Runtime API configuration is a deep snapshot, not a live mutable view")]
    public void RuntimeSnapshotDoesNotShareMutableCollections()
    {
        var retryStatuses = new[] { 429 };
        var sensitiveKeys = new[] { "token" };
        var authHeaders = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Correlation"] = "original"
        };
        var settings = new ApiSettings
        {
            BaseUrl = "https://example.test",
            RateLimit = new RateLimitSettings { RetryStatuses = retryStatuses },
            Redaction = new RedactionSettings { SensitiveKeys = sensitiveKeys },
            AuthProfiles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["custom"] = new AuthProfileSettings
                {
                    Type = "custom",
                    Headers = authHeaders
                }
            }
        };

        var snapshot = settings.ToRuntimeSnapshot();

        retryStatuses[0] = 500;
        sensitiveKeys[0] = "email";
        authHeaders["X-Correlation"] = "mutated";
        settings.AuthProfiles.Clear();

        Assert.Equal("https://example.test", snapshot.BaseUrl);
        Assert.Equal([429], snapshot.RateLimit.RetryStatuses);
        Assert.Equal(["token"], snapshot.Redaction.SensitiveKeys);
        Assert.Equal("original", snapshot.AuthProfiles["custom"].Headers["X-Correlation"]);
    }
}
