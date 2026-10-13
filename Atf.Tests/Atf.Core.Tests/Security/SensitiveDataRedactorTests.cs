using Atf.Core.Configuration.Security;
using Atf.Core.Security;
using System.Text.RegularExpressions;

namespace Atf.Core.Tests.Security;

public sealed class SensitiveDataRedactorTests
{
    [Fact(DisplayName = "Redaction facade composes focused strategies instead of owning regex state")]
    public void FacadeUsesFocusedRedactionStrategies()
    {
        var fields = typeof(SensitiveDataRedactor).GetFields(
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);

        Assert.DoesNotContain(fields, field => typeof(Regex).IsAssignableFrom(field.FieldType));
        Assert.Contains(fields, field => field.FieldType.Name == "SecretPatternRedactor");
        Assert.Contains(fields, field => field.FieldType.Name == "QueryStringRedactor");
        Assert.Contains(fields, field => field.FieldType.Name == "LocalPathRedactor");
        Assert.Contains(fields, field => field.FieldType.Name == "PiiRedactor");
        Assert.Contains(fields, field => field.FieldType.Name == "StructuredJsonRedactor");
    }

    [Fact(DisplayName = "Redactor masks structured secrets but keeps business PII diagnostic by default")]
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
        Assert.Contains("person@example.test", result, StringComparison.Ordinal);
        Assert.Contains("secondary@example.test", result, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "PII masking is explicitly configurable")]
    public void RedactsPiiOnlyWhenEnabled()
    {
        var redactor = new SensitiveDataRedactor(new RedactionSettings
        {
            PiiEnabled = true,
            Replacement = "[PII]"
        });

        var result = redactor.RedactJson("{\"email\":\"person@example.test\",\"phone\":\"+401234\",\"active\":true,\"count\":3}");

        Assert.DoesNotContain("person@example.test", result, StringComparison.Ordinal);
        Assert.DoesNotContain("+401234", result, StringComparison.Ordinal);
        Assert.Contains("[PII]", result, StringComparison.Ordinal);
        Assert.Contains("true", result, StringComparison.Ordinal);
        Assert.Contains("3", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Global redaction switch cannot disable secret masking")]
    public void DisabledOptionalRedactionStillMasksSecrets()
    {
        var redactor = new SensitiveDataRedactor(new RedactionSettings { Enabled = false });
        const string payload = "Authorization: Bearer secret-value; owner=person@example.test";

        var result = redactor.RedactText(payload);

        Assert.DoesNotContain("secret-value", result, StringComparison.Ordinal);
        Assert.Contains("person@example.test", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Global redaction switch cannot disable structured secret masking")]
    public void DisabledOptionalRedactionStillMasksStructuredSecrets()
    {
        var redactor = new SensitiveDataRedactor(new RedactionSettings { Enabled = false });

        var result = redactor.RedactJson("{\"password\":\"secret-value\",\"owner\":\"person@example.test\"}");

        Assert.DoesNotContain("secret-value", result, StringComparison.Ordinal);
        Assert.Contains("person@example.test", result, StringComparison.Ordinal);
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
        Assert.Contains("person@example.test", text, StringComparison.Ordinal);
        Assert.Contains("visible=ok", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Redactor protects malformed or plain-text payloads")]
    public void RedactsNonJsonPayload()
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.RedactJson("password=plain-secret; owner=person@example.test");

        Assert.DoesNotContain("plain-secret", result, StringComparison.Ordinal);
        Assert.Contains("person@example.test", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Cookie and Set-Cookie values are always treated as secrets")]
    public void RedactsCookieHeaders()
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.RedactText("Cookie: session=secret-cookie; Set-Cookie: refresh_token=secret-refresh");

        Assert.DoesNotContain("secret-cookie", result, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-refresh", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Redactor preserves non-sensitive values in large JSON bodies")]
    public void PreservesLargeNonSensitiveJson()
    {
        var payload = "[" + string.Join(',', Enumerable.Range(1, 250).Select(index =>
            $"{{\"id\":{index},\"label\":\"business-{index}\"}}")) + "]";

        var result = new SensitiveDataRedactor().RedactJson(payload);

        Assert.Contains("business-1", result, StringComparison.Ordinal);
        Assert.Contains("business-250", result, StringComparison.Ordinal);
        Assert.DoesNotContain("[REDACTED]", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Redactor masks multiline exceptions and URL encoded secrets")]
    public void RedactsMultilineExceptionAndEncodedSecret()
    {
        var redactor = new SensitiveDataRedactor(new RedactionSettings
        {
            SensitiveKeys = ["token", "api_key"]
        });

        var result = redactor.RedactText(
            "Exception: Bearer exception-token\n" +
            "https://example.test/items?api_key=encoded%2Fsecret&visible=yes");

        Assert.DoesNotContain("exception-token", result, StringComparison.Ordinal);
        Assert.DoesNotContain("encoded%2Fsecret", result, StringComparison.Ordinal);
        Assert.Contains("visible=yes", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Redactor removes absolute Windows paths from exception evidence")]
    public void RedactsAbsoluteWindowsPaths()
    {
        var redactor = new SensitiveDataRedactor();

        var result = redactor.RedactText(
            "at Demo.Run() in C:\\work\\ex\\.net\\Atf\\Atf.Api\\Tests\\FailApiTests.cs:line 40");

        Assert.DoesNotContain("C:\\work\\ex", result, StringComparison.Ordinal);
        Assert.Contains("[LOCAL_PATH]:line 40", result, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Absolute path redaction is configurable for local diagnostics")]
    public void CanPreserveAbsoluteWindowsPathsWhenExplicitlyDisabled()
    {
        var redactor = new SensitiveDataRedactor(new RedactionSettings { RedactLocalPaths = false });

        const string path = "C:\\work\\ex\\Atf\\file.cs:line 12";
        Assert.Contains(path, redactor.RedactText($"at Demo.Run() in {path}"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Redactor masks secrets inside nested arrays and malformed bodies")]
    public void RedactsNestedArraysAndMalformedBodies()
    {
        var redactor = new SensitiveDataRedactor();

        var nested = redactor.RedactJson("[{\"items\":[{\"password\":\"nested-secret\"}]}]");
        var malformed = redactor.RedactJson("{ password: malformed-secret, owner: person@example.test }");

        Assert.DoesNotContain("nested-secret", nested, StringComparison.Ordinal);
        Assert.DoesNotContain("malformed-secret", malformed, StringComparison.Ordinal);
        Assert.Contains("person@example.test", malformed, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Redactor masks custom configured header keys")]
    public void RedactsCustomConfiguredHeaders()
    {
        var redactor = new SensitiveDataRedactor(new RedactionSettings
        {
            SensitiveKeys = ["x_internal_secret"]
        });

        var result = redactor.RedactText("X_INTERNAL_SECRET: custom-secret; visible: ok");

        Assert.DoesNotContain("custom-secret", result, StringComparison.Ordinal);
        Assert.Contains("visible: ok", result, StringComparison.Ordinal);
    }
}
