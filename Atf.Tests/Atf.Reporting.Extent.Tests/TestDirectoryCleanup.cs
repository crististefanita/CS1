namespace Atf.Reporting.Extent.Tests;

internal static class TestDirectoryCleanup
{
    public static void DeleteEventually(DirectoryInfo directory)
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            try
            {
                if (!directory.Exists)
                {
                    return;
                }

                directory.Delete(recursive: true);
                return;
            }
            catch (IOException) when (attempt < 11)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 11)
            {
                Thread.Sleep(100);
            }
        }

        directory.Delete(recursive: true);
    }
}
