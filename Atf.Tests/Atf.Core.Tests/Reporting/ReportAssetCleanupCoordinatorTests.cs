using Atf.Core.Reporting.Artifacts;
using System.Collections.Concurrent;

namespace Atf.Core.Tests.Reporting;

public sealed class ReportAssetCleanupCoordinatorTests
{
    [Fact(DisplayName = "Asset cleanup retries transient renderer locks without sleeping in the test")]
    public void RetriesTransientDeleteFailure()
    {
        var fileSystem = new FakeFileSystem(failuresBeforeDelete: 2);
        var coordinator = new ReportAssetCleanupCoordinator(
            fileSystem,
            [TimeSpan.Zero, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2)]);

        coordinator.CleanDirectory("renderer-assets");

        Assert.Equal(3, fileSystem.DeleteAttempts);
        Assert.Equal(2, fileSystem.Delays.Count);
        Assert.False(fileSystem.Exists);
        Assert.Equal(0, ReportAssetCleanupCoordinator.ActiveDirectoryGateCount);
    }

    [Fact(DisplayName = "Concurrent cleanup of one renderer directory is serialized")]
    public async Task SameDirectoryIsDeletedOnceWhenCleanupRunsConcurrently()
    {
        var fileSystem = new FakeFileSystem();
        var coordinator = new ReportAssetCleanupCoordinator(fileSystem, [TimeSpan.Zero]);

        await Task.WhenAll(
            Task.Run(() => coordinator.CleanDirectory("renderer-assets"), TestContext.Current.CancellationToken),
            Task.Run(() => coordinator.CleanDirectory("renderer-assets"), TestContext.Current.CancellationToken),
            Task.Run(() => coordinator.CleanDirectory("renderer-assets"), TestContext.Current.CancellationToken));

        Assert.Equal(1, fileSystem.DeleteAttempts);
        Assert.False(fileSystem.Exists);
        Assert.Equal(0, ReportAssetCleanupCoordinator.ActiveDirectoryGateCount);
    }

    [Fact(DisplayName = "Parallel sessions delete only their own reserved renderer directories")]
    public async Task ParallelSessionsDoNotCrossDeleteDirectories()
    {
        var firstFileSystem = new FakeFileSystem();
        var secondFileSystem = new FakeFileSystem();
        var first = new ReportAssetCleanupCoordinator(firstFileSystem, [TimeSpan.Zero]);
        var second = new ReportAssetCleanupCoordinator(secondFileSystem, [TimeSpan.Zero]);

        await Task.WhenAll(
            Task.Run(() => first.CleanDirectory("session-a/renderer-assets"), TestContext.Current.CancellationToken),
            Task.Run(() => second.CleanDirectory("session-b/renderer-assets"), TestContext.Current.CancellationToken));

        Assert.Equal(["session-a/renderer-assets"], firstFileSystem.DeletedPaths);
        Assert.Equal(["session-b/renderer-assets"], secondFileSystem.DeletedPaths);
        Assert.Equal(0, ReportAssetCleanupCoordinator.ActiveDirectoryGateCount);
    }

    [Fact(DisplayName = "Repeated cleanup is idempotent after the first successful delete")]
    public void RepeatedCleanupDoesNotDeleteAnAlreadyRemovedDirectory()
    {
        var fileSystem = new FakeFileSystem();
        var coordinator = new ReportAssetCleanupCoordinator(fileSystem, [TimeSpan.Zero]);

        coordinator.CleanDirectory("renderer-assets");
        coordinator.CleanDirectory("renderer-assets");

        Assert.Equal(1, fileSystem.DeleteAttempts);
        Assert.Equal(0, ReportAssetCleanupCoordinator.ActiveDirectoryGateCount);
    }

    [Fact(DisplayName = "Missing renderer directory is a successful no-op")]
    public void MissingDirectoryDoesNotInvokeDelete()
    {
        var fileSystem = new FakeFileSystem { Exists = false };
        var coordinator = new ReportAssetCleanupCoordinator(fileSystem, [TimeSpan.Zero]);

        coordinator.CleanDirectory("renderer-assets");

        Assert.Equal(0, fileSystem.DeleteAttempts);
        Assert.Equal(0, ReportAssetCleanupCoordinator.ActiveDirectoryGateCount);
    }

    [Fact(DisplayName = "Exhausted I/O cleanup retries do not leak the directory gate")]
    public void ExhaustedIoRetriesReleaseDirectoryGate()
    {
        var fileSystem = new FakeFileSystem(failuresBeforeDelete: 5);
        var coordinator = new ReportAssetCleanupCoordinator(
            fileSystem,
            [TimeSpan.Zero, TimeSpan.FromMilliseconds(1)]);

        coordinator.CleanDirectory("renderer-assets");

        Assert.Equal(2, fileSystem.DeleteAttempts);
        Assert.True(fileSystem.Exists);
        Assert.Single(fileSystem.Delays);
        Assert.Equal(0, ReportAssetCleanupCoordinator.ActiveDirectoryGateCount);
    }

    [Fact(DisplayName = "Exhausted unauthorized cleanup retries do not mask the report verdict")]
    public void ExhaustedUnauthorizedRetriesReleaseDirectoryGate()
    {
        var fileSystem = new FakeFileSystem(
            failuresBeforeDelete: 5,
            useUnauthorizedAccessException: true);
        var coordinator = new ReportAssetCleanupCoordinator(
            fileSystem,
            [TimeSpan.Zero, TimeSpan.FromMilliseconds(1)]);

        var exception = Record.Exception(() => coordinator.CleanDirectory("renderer-assets"));

        Assert.Null(exception);
        Assert.Equal(2, fileSystem.DeleteAttempts);
        Assert.True(fileSystem.Exists);
        Assert.Equal(0, ReportAssetCleanupCoordinator.ActiveDirectoryGateCount);
    }

    private sealed class FakeFileSystem(
        int failuresBeforeDelete = 0,
        bool useUnauthorizedAccessException = false) : IReportAssetFileSystem
    {
        private int _remainingFailures = failuresBeforeDelete;
        private readonly bool _useUnauthorizedAccessException = useUnauthorizedAccessException;

        public bool Exists { get; set; } = true;
        public ConcurrentQueue<TimeSpan> Delays { get; } = new();
        public ConcurrentQueue<string> DeletedPaths { get; } = new();

        public bool DirectoryExists(string directoryPath) => Exists;

        public void DeleteDirectory(string directoryPath)
        {
            Interlocked.Increment(ref _deleteAttempts);
            DeletedPaths.Enqueue(directoryPath);
            if (_remainingFailures-- > 0)
            {
                if (_useUnauthorizedAccessException)
                {
                    throw new UnauthorizedAccessException("simulated renderer permission race");
                }

                throw new IOException("simulated renderer lock");
            }

            Exists = false;
        }

        private int _deleteAttempts;

        public int DeleteAttempts => Volatile.Read(ref _deleteAttempts);

        public void Delay(TimeSpan delay) => Delays.Enqueue(delay);
    }
}
