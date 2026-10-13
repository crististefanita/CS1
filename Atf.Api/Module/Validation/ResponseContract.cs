using Atf.Core.Http;
using Atf.Common.Validation.Contracts;
using System.Net;

namespace Atf.Common.Validation;

/// <summary>
/// Contract for exactly one <see cref="ApiResponse"/>.
/// One response owns one ordered rule list and one aggregate table. A
/// contract is single-use after evaluation, preventing accidental aggregation
/// of evidence from different HTTP responses.
/// </summary>
public sealed class ResponseContract
{
    private readonly List<IResponseValidationRule> _rules = [];
    private readonly HashSet<string> _ruleKeys = new(StringComparer.Ordinal);
    private bool _evaluated;

    public IReadOnlyList<IResponseValidationRule> Rules => _rules;

    // The short fluent names are the public validation DSL. The explicit
    // Expect* methods remain available for reusable contract factories.
    public ResponseContract Status(HttpStatusCode expected) => ExpectStatus(expected);
    public ResponseContract StatusOneOf(params HttpStatusCode[] expected) => ExpectStatusOneOf(expected);
    public ResponseContract ContentTypeContains(string expected) => ExpectContentType(expected);
    public ResponseContract HeaderPresent(string name) => ExpectHeader(name);
    public ResponseContract BodyContains(string expected) => ExpectBodyContains(expected);
    public ResponseContract JsonPathExists(string path) => ExpectJsonPath(path);
    public ResponseContract JsonPathEquals(string path, string expected) => ExpectJsonPath(path, expected);
    public ResponseContract EveryArrayItemEquals(string path, string property, string expected) => ExpectEveryArrayItem(path, property, expected);
    public ResponseContract JsonSchema(string schemaPath) => ExpectSchema(schemaPath);

    public ResponseContract ExpectStatus(HttpStatusCode expected)
    {
        EnsureBuildable();
        AddRule($"status:{expected}", new DelegateRule("HTTP status", (response, table) => ValidationChecks.Status(table, response, expected)));
        return this;
    }

    public ResponseContract ExpectStatusOneOf(params HttpStatusCode[] expected)
    {
        ArgumentNullException.ThrowIfNull(expected);
        EnsureBuildable();
        var allowed = expected.ToArray();
        AddRule($"status-one-of:{string.Join(',', allowed)}", new DelegateRule("HTTP status", (response, table) => ValidationChecks.StatusOneOf(table, response, allowed)));
        return this;
    }

    public ResponseContract ExpectContentType(string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);
        EnsureBuildable();
        AddRule($"content-type:{expected.Trim()}", new DelegateRule("Content-Type", (response, table) => ValidationChecks.ContentTypeContains(table, response, expected)));
        return this;
    }

    public ResponseContract ExpectHeader(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        EnsureBuildable();
        AddRule($"header:{name.Trim().ToUpperInvariant()}", new DelegateRule($"Header {name}", (response, table) => ValidationChecks.HeaderPresent(table, response, name)));
        return this;
    }

    public ResponseContract ExpectBodyContains(string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expected);
        EnsureBuildable();
        AddRule($"body-contains:{expected}", new DelegateRule("Response body", (response, table) => ValidationChecks.BodyContains(table, response, expected)));
        return this;
    }

    public ResponseContract ExpectJsonPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        EnsureBuildable();
        AddRule($"json-path-exists:{path.Trim()}", new DelegateRule($"JSONPath exists: {path}", (response, table) => ValidationChecks.JsonPathExists(table, response, path)));
        return this;
    }

    public ResponseContract ExpectJsonPath(string path, string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(expected);
        EnsureBuildable();
        AddRule($"json-path-equals:{path.Trim()}:{expected}", new DelegateRule($"JSONPath equals: {path}", (response, table) => ValidationChecks.JsonPathEquals(table, response, path, expected)));
        return this;
    }

    public ResponseContract ExpectEveryArrayItem(string path, string property, string expected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(property);
        ArgumentNullException.ThrowIfNull(expected);
        EnsureBuildable();
        AddRule($"every-array:{path.Trim()}:{property.Trim()}:{expected}", new DelegateRule($"Every {path} item {property}", (response, table) => ValidationChecks.EveryArrayItemEquals(table, response, path, property, expected)));
        return this;
    }

    public ResponseContract ExpectSchema(string schemaPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaPath);
        EnsureBuildable();
        AddRule($"schema:{schemaPath.Trim()}", new DelegateRule("JSON schema", (response, table) => ValidationChecks.JsonSchema(table, response, schemaPath)));
        return this;
    }

    public void Evaluate(ApiResponse response, ValidationTable table)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(table);
        if (_evaluated)
        {
            throw new InvalidOperationException("A response contract can be evaluated only once.");
        }

        if (_rules.Count == 0)
        {
            throw new InvalidOperationException("A response contract must contain at least one validation rule.");
        }

        _evaluated = true;
        foreach (var rule in _rules)
        {
            rule.Evaluate(response, table);
        }
    }

    private void EnsureBuildable()
    {
        if (_evaluated)
        {
            throw new InvalidOperationException("A response contract cannot be changed after evaluation.");
        }
    }

    private void AddRule(string key, IResponseValidationRule rule)
    {
        if (!_ruleKeys.Add(key))
        {
            throw new InvalidOperationException($"Duplicate response validation rule '{rule.Name}' is not allowed in one contract.");
        }

        _rules.Add(rule);
    }

    private sealed class DelegateRule(string name, Action<ApiResponse, ValidationTable> evaluator) : IResponseValidationRule
    {
        public string Name { get; } = name;

        public void Evaluate(ApiResponse response, ValidationTable table) => evaluator(response, table);
    }
}
