using Atf.Core.Reporting;

namespace Atf.Common.Lifecycle;

public sealed class CleanupRegistry(IAtfReporter reporter)
{
    private readonly List<(string Resource, string Id, Func<Task> Action)> _items = [];

    public bool HasPending => _items.Count > 0;

    public void Own(string resource, string id, Func<Task> action) => _items.Add((resource, id, action));

    public async Task RunAsync()
    {
        foreach (var item in Enumerable.Reverse(_items))
        {
            reporter.StartStep($"Cleanup: DELETE {item.Resource}/{item.Id}");
            try
            {
                await item.Action();
                reporter.Pass("Cleanup completed.");
            }
            catch (Exception exception)
            {
                reporter.Warning($"Best-effort cleanup failed for {item.Resource}/{item.Id}: {exception.Message}");
            }
        }

        _items.Clear();
    }
}
