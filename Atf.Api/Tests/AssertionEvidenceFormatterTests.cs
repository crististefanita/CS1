using Atf.Common.Validation;
using Atf.Core.Reporting;
using Xunit;

namespace Atf.Api;

public sealed class AssertionEvidenceFormatterTests
{
    [Fact(DisplayName = "Assertion table renders expected, actual and result in separate cells")]
    public void RendersCompleteAssertionContract()
    {
        var html = AssertionEvidenceFormatter.BuildTableHtml(
        [
            new AtfAssertion("HTTP status", "200 OK", "201 Created", false),
            new AtfAssertion("JSON schema", "user.schema.json", "schema matched", true)
        ]);

        Assert.Contains("<th>ASSERTION</th><th>EXPECTED</th><th>ACTUAL</th><th>RESULT</th>", html);
        Assert.Contains("<td>200 OK</td><td>201 Created</td><td>", html);
        Assert.Contains("<td>user.schema.json</td><td>schema matched</td><td>", html);
        Assert.Contains("assertion-result-fail", html);
        Assert.Contains("assertion-result-pass", html);
    }

    [Fact(DisplayName = "Assertion table HTML-encodes actual diagnostics")]
    public void EncodesActualValueWithoutDroppingIt()
    {
        var html = AssertionEvidenceFormatter.BuildTableHtml(
        [
            new AtfAssertion("Body", "valid", "<redacted> & retry", false)
        ]);

        Assert.Contains("&lt;redacted&gt; &amp; retry", html);
    }

    [Fact(DisplayName = "Validation table preserves actual value in its row model")]
    public void ValidationTablePreservesActualValue()
    {
        var table = new ValidationTable();
        table.Check("HTTP status", "200 OK", "503 Service Unavailable", () => false);

        var row = Assert.Single(table.Rows);
        Assert.Equal("503 Service Unavailable", row.Actual);
        Assert.False(row.Passed);
    }

    [Fact(DisplayName = "Technical trace keeps the compact exchange contract")]
    public void TechnicalTraceKeepsRelevantExchangeFields()
    {
        var html = EvidenceTableFormatter.BuildTechnicalTraceTableHtml(
        [
            new KeyValuePair<string, string>("route.name", "users.collection"),
            new KeyValuePair<string, string>("request.type", "HTTP GET"),
            new KeyValuePair<string, string>("method", "GET"),
            new KeyValuePair<string, string>("path", "/public/v2/users"),
            new KeyValuePair<string, string>("url", "https://api.test/public/v2/users"),
            new KeyValuePair<string, string>("route.path.template", "/public/v2/users/{id}"),
            new KeyValuePair<string, string>("route.auth.type", "none"),
            new KeyValuePair<string, string>("route.auth.requirement", "optional"),
            new KeyValuePair<string, string>("route.auth.profile", "anonymous"),
            new KeyValuePair<string, string>("auth.profile", "anonymous"),
            new KeyValuePair<string, string>("auth.mode", "none"),
            new KeyValuePair<string, string>("auth.token.state", "not applicable"),
            new KeyValuePair<string, string>("attempt", "1"),
            new KeyValuePair<string, string>("response.status", "200"),
            new KeyValuePair<string, string>("response.content.type", "application/json"),
            new KeyValuePair<string, string>("response.duration.ms", "1750"),
            new KeyValuePair<string, string>("exchange.duration.ms", "1751")
        ]);

        Assert.Contains("class='helm-evidence-table helm-trace-table'", html);
        Assert.Contains("<th>FIELD</th><th>VALUE</th>", html);
        Assert.Contains("helm-trace-table", html);
        Assert.Contains("<th scope='row'>METHOD</th><td data-field='method'>GET</td>", html);
        Assert.Contains("<th scope='row'>ENDPOINT</th><td data-field='url'>https://api.test/public/v2/users</td>", html);
        Assert.Contains("<th scope='row'>ROUTE TEMPLATE</th><td data-field='route.path.template'>/public/v2/users/{id}</td>", html);
        Assert.Contains("<th scope='row'>ROUTE AUTH TYPE</th><td data-field='route.auth.type'>none</td>", html);
        Assert.Contains("<th scope='row'>ROUTE AUTH ACCESS</th><td data-field='route.auth.requirement'>optional</td>", html);
        Assert.Contains("<th scope='row'>ROUTE AUTH PROFILE</th><td data-field='route.auth.profile'>anonymous</td>", html);
        Assert.Contains("<th scope='row'>AUTHENTICATION</th><td data-field='auth.profile'>anonymous</td>", html);
        Assert.Contains("<th scope='row'>ATTEMPT</th><td data-field='attempt'>1</td>", html);
        Assert.Contains("<th scope='row'>RESPONSE STATUS</th><td data-field='response.status'>200</td>", html);
        Assert.Contains("<th scope='row'>CONTENT TYPE</th><td data-field='response.content.type'>application/json</td>", html);
        Assert.Contains("<th scope='row'>RESPONSE DURATION</th><td data-field='response.duration.ms'>1750</td>", html);
        Assert.DoesNotContain("<th scope='row'>ROUTE</th>", html);
        Assert.DoesNotContain("REQUEST TYPE", html);
        Assert.DoesNotContain("AUTH MODE", html);
        Assert.DoesNotContain("TOKEN STATE", html);
        Assert.DoesNotContain("TOTAL DURATION", html);
    }

    [Fact(DisplayName = "Technical trace retains only meaningful retry diagnostics")]
    public void TechnicalTraceRetainsRetryEvidence()
    {
        var details = EvidenceTableFormatter.CompactTechnicalTraceDetails(
        [
            new KeyValuePair<string, string>("method", "GET"),
            new KeyValuePair<string, string>("url", "https://api.test/items"),
            new KeyValuePair<string, string>("auth.profile", "default"),
            new KeyValuePair<string, string>("attempt", "2"),
            new KeyValuePair<string, string>("response.status", "200"),
            new KeyValuePair<string, string>("response.duration.ms", "150"),
            new KeyValuePair<string, string>("exchange.duration.ms", "5200"),
            new KeyValuePair<string, string>("rate-limit.retry.delay.ms", "5000"),
            new KeyValuePair<string, string>("rate-limit.retry.reason", "Retry-After")
        ]);

        Assert.Contains(details, item => item.Key == "attempt" && item.Value == "2");
        Assert.Contains(details, item => item.Key == "exchange.duration.ms" && item.Value == "5200");
        Assert.Contains(details, item => item.Key == "rate-limit.retry.delay.ms");
        Assert.Contains(details, item => item.Key == "rate-limit.retry.reason");
    }

    [Fact(DisplayName = "Technical trace deduplicates repeated executor fields")]
    public void TechnicalTraceDeduplicatesRepeatedFields()
    {
        var details = EvidenceTableFormatter.CompactTechnicalTraceDetails(
        [
            new KeyValuePair<string, string>("method", "GET"),
            new KeyValuePair<string, string>("method", "GET"),
            new KeyValuePair<string, string>("url", "https://api.test/items"),
            new KeyValuePair<string, string>("response.status", "200"),
            new KeyValuePair<string, string>("response.status", "200"),
            new KeyValuePair<string, string>("response.content.type", "application/json"),
            new KeyValuePair<string, string>("response.duration.ms", "120")
        ]);

        Assert.Equal(5, details.Count);
        Assert.Single(details, item => item.Key == "method" && item.Value == "GET");
        Assert.Single(details, item => item.Key == "response.status" && item.Value == "200");
    }

    [Fact(DisplayName = "Technical trace encodes diagnostic values")]
    public void TechnicalTraceEncodesValues()
    {
        var html = EvidenceTableFormatter.BuildTechnicalTraceTableHtml(
        [
            new KeyValuePair<string, string>("url", "https://api.test/items?id=1&amp;x=2")
        ]);

        Assert.Contains("https://api.test/items?id=1&amp;amp;x=2", html);
    }
}
