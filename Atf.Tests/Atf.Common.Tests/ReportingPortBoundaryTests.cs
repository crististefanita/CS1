using Atf.Common.Lifecycle;
using Atf.Common.Modules;
using Atf.Common.Validation;
using Atf.Core.Reporting;

namespace Atf.Common.Tests;

public sealed class ReportingPortBoundaryTests
{
    [Fact(DisplayName = "Common consumers use narrow reporting ports instead of the aggregate façade")]
    public void CommonConsumersDoNotDependOnAggregateReporter()
    {
        var aggregate = typeof(IAtfReporter);
        var commonAssembly = typeof(ApiModule).Assembly;
        var consumers = commonAssembly
            .GetTypes()
            .Where(type => type.IsClass && type.Namespace?.StartsWith("Atf.Common", StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetConstructors())
            .SelectMany(constructor => constructor.GetParameters())
            .Where(parameter => parameter.ParameterType == aggregate)
            .Select(parameter => parameter.Member.DeclaringType?.FullName)
            .Where(name => name is not null)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(consumers);
        Assert.Equal(typeof(IAtfValidationReporter), typeof(ApiModule)
            .GetConstructor([typeof(Atf.Core.Configuration.Models.ApiSettings), typeof(IAtfValidationReporter), typeof(HttpClient)])!
            .GetParameters()[1].ParameterType);
        Assert.Equal(typeof(IAtfCleanupReporter), typeof(CleanupRegistry).GetConstructors().Single()
            .GetParameters()[0].ParameterType);
    }
}
