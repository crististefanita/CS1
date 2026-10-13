using Atf.Core.Http;

namespace Atf.Common.Validation.Contracts;

/// <summary>
/// One framework-neutral rule evaluated against exactly one HTTP response.
/// Rules never write to Extent or another reporter; they only append one row
/// to the response-owned validation table.
/// </summary>
public interface IResponseValidationRule
{
    string Name { get; }

    void Evaluate(ApiResponse response, ValidationTable table);
}
