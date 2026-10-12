using Atf.Core.Http;
using Atf.Core.Reporting;

namespace Atf.Common.Validation;

public static class JsonSchemaAssertions
{
    public static void Matches(IAtfReporter reporter, ApiResponse response, string schemaPath)
    {
        ApiAssertions.Contract(reporter, response, "Validate: JSON schema",
            group => group.JsonSchema(schemaPath));
    }
}
