using Atf.Core.Configuration;
using Atf.Core.Security;

namespace Atf.Core.Tests.Security;

public sealed class SensitiveDataRedactorTests
{
    [Fact(DisplayName = "Redactor masks structured secret keys and embedded email values")]
    public void RedactsStructuredJson()
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.RedactJson("""
            {
              "access_token": "token-value",
              "profile": { "email": "person@example.test", "display": "contact person@example.test" },
              "items": ["secondary@example.test"]
            }
            """);

        Assert.DoesNotContain("token-value", result, StringComparison.Ordinal);
        Assert.DoesNotContain("person@example.test", result, StringComparison.Ordinal);
        Assert.DoesNotContain("secondary@example.test", result, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Redactor masks authorization headers, bearer values and query secrets")]
    public void RedactsFreeTextAndQueryValues()
    {
        var redactor = new SensitiveDataRedactor(new RedactionSettings
        {
            SensitiveKeys = ["token", "api_key"]
        });

        var result = redactor.RedactField(
            "url",
            "https://example.test/items?token=secret-value&visible=ok");
        var text = redactor.RedactText(
            "Authorization: Bearer bearer-secret; X-Api-Key=api-secret; mail=person@example.test");

        Assert.DoesNotContain("secret-value", result, StringComparison.Ordinal);
        Assert.DoesNotContain("bearer-secret", text, StringComparison.Ordinal);
        Assert.DoesNotContain("api-secret", text, StringComparison.Ordinal);
        Assert.DoesNotContain("person@example.test", text, StringComparison.Ordinal);
        Assert.Contains("visible=ok", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Redactor protects malformed or plain-text payloads")]
    public void RedactsNonJsonPayload()
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.RedactJson("password=plain-secret; owner=person@example.test");

        Assert.DoesNotContain("plain-secret", result, StringComparison.Ordinal);
        Assert.DoesNotContain("person@example.test", result, StringComparison.Ordinal);
    }
}
