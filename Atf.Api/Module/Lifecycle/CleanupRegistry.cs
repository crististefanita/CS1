using Atf.Core.Reporting;

namespace Atf.Common.Lifecycle;

public sealed class CleanupRegistry(IAtfCleanupReporter reporter)
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
            var title = $"Cleanup: DELETE {item.Resource}/{item.Id}";
            var status = AtfReportStatus.Pass;

            // Cleanup owns the resource; reporting is only diagnostic. A
            // broken reporter must never prevent the action from running or
            // stop the registry from releasing the next resource.
            var startFailure = TryReport(() => reporter.StartStep(title));
            try
            {
                await item.Action();
                if (startFailure is null)
                {
                    var passFailure = TryReport(() => reporter.Pass("Cleanup completed."));
                    if (passFailure is not null)
                    {
                        status = AtfReportStatus.Warning;
                        TryReport(() => reporter.Warning(
                            $"Cleanup completed but success reporting failed: {passFailure.Message}"));
                    }
                }
                else
                {
                    status = AtfReportStatus.Warning;
                    TryReport(() => reporter.Warning(
                        $"Cleanup completed but reporting could not open the step: {startFailure.Message}"));
                }
            }
            catch (Exception exception)
            {
                status = AtfReportStatus.Warning;
                TryReport(() => reporter.Warning(
                    $"Best-effort cleanup failed for {item.Resource}/{item.Id}: {exception.Message}"));
            }
            finally
            {
                TryReport(() => reporter.CompleteStep(status));
            }
        }
    }

    private static Exception? TryReport(Action report)
    {
        try
        {
            report();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
