using Atf.Core.Http;
using System.Net;

namespace Atf.Common.Validation.Groups;

/// <summary>
/// Fluent assertion scope for one HTTP response.
/// Every operation adds a row to the same <see cref="ValidationTable"/>.
/// It must not be used to combine evidence from different responses.
/// </summary>
public sealed class ResponseValidationGroup
{
    private readonly ResponseContract _contract;

    internal ResponseValidationGroup(ResponseContract contract)
    {
        _contract = contract;
    }

    public ResponseValidationGroup Status(HttpStatusCode expected)
    {
        _contract.ExpectStatus(expected);
        return this;
    }

    public ResponseValidationGroup StatusOneOf(params HttpStatusCode[] expected)
    {
        _contract.ExpectStatusOneOf(expected);
        return this;
    }

    public ResponseValidationGroup ContentTypeContains(string expected)
    {
        _contract.ExpectContentType(expected);
        return this;
    }

    public ResponseValidationGroup HeaderPresent(string name)
    {
        _contract.ExpectHeader(name);
        return this;
    }

    public ResponseValidationGroup BodyContains(string expected)
    {
        _contract.ExpectBodyContains(expected);
        return this;
    }

    public ResponseValidationGroup JsonPathExists(string path)
    {
        _contract.ExpectJsonPath(path);
        return this;
    }

    public ResponseValidationGroup JsonPathEquals(string path, string expected)
    {
        _contract.ExpectJsonPath(path, expected);
        return this;
    }

    public ResponseValidationGroup EveryArrayItemEquals(string path, string property, string expected)
    {
        _contract.ExpectEveryArrayItem(path, property, expected);
        return this;
    }

    public ResponseValidationGroup JsonSchema(string schemaPath)
    {
        _contract.ExpectSchema(schemaPath);
        return this;
    }
}
