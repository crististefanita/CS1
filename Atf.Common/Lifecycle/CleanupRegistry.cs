using Atf.Core.Reporting;

namespace Atf.Common.Lifecycle;

public sealed class CleanupRegistry(IAtfReporter reporter)
{
    private readonly object _sync = new();
    private readonly List<(string Resource, string Id, Func<Task> Action)> _items = [];

    public bool HasPending
    {
        get
        {
            lock (_sync)
            {
                return _items.Count > 0;
            }
        }
    }

    public void Own(string resource, string id, Func<Task> action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(action);
        lock (_sync)
        {
            _items.Add((resource, id, action));
        }
    }

    public async Task RunAsync()
    {
        (string Resource, string Id, Func<Task> Action)[] items;
        lock (_sync)
        {
            items = _items.AsEnumerable().Reverse().ToArray();
            _items.Clear();
        }

        foreach (var item in items)
        {
            reporter.StartStep($"Cleanup: DELETE {item.Resource}/{item.Id}");
            var status = AtfReportStatus.Pass;
            try
            {
                await item.Action();
                reporter.Pass("Cleanup completed.");
            }
            catch (Exception exception)
            {
                status = AtfReportStatus.Warning;
                reporter.Warning($"Best-effort cleanup failed for {item.Resource}/{item.Id}: {exception.Message}");
            }
            finally
            {
                reporter.CompleteStep(status);
            }
        }
    }
}
