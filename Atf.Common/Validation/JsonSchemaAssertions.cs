using Atf.Core.Http;
using Atf.Core.Reporting;

namespace Atf.Common.Validation;

public static class JsonSchemaAssertions
{
    public static void Matches(IAtfReporter reporter, ApiResponse response, string schemaPath)
    {
        ValidationTable.Run(reporter, "Validate: JSON schema", table =>
            ValidationChecks.JsonSchema(table, response, schemaPath));
    }
}
