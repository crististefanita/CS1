using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace Atf.Core.Tests.Architecture;

public sealed class AnnotationPolicyTests
{
    [Fact(DisplayName = "API scenarios do not use the generic framework tag")]
    public void GenericFrameworkTagIsNotUsedByScenariosOrTagOrder()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceFiles = Directory.EnumerateFiles(
                Path.Combine(repositoryRoot, "Atf.Api", "Tests"),
                "*.cs",
                SearchOption.AllDirectories)
            .Append(Path.Combine(repositoryRoot, "Atf.Core", "Configuration", "appsettings.json"));

        var forbiddenTag = new Regex(
            "(?:[\\\"'])(?:framework)(?:[\\\"'])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var violations = sourceFiles
            .SelectMany(path => forbiddenTag.Matches(File.ReadAllText(path))
                .Select(match => $"{Path.GetRelativePath(repositoryRoot, path)}:{match.Index}"))
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }

    private static string FindRepositoryRoot([CallerFilePath] string sourceFile = "")
    {
        var candidates = new[]
        {
            new DirectoryInfo(Path.GetDirectoryName(sourceFile) ?? string.Empty),
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(AppContext.BaseDirectory)
        }.Where(directory => directory.Exists);

        foreach (var candidate in candidates)
        {
            var directory = candidate;
            while (directory is not null &&
                   !File.Exists(Path.Combine(directory.FullName, "Atf.slnx")))
            {
                directory = directory.Parent;
            }

            if (directory is not null)
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the ATF repository root.");
    }
}
