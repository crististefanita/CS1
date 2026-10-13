namespace Atf.Common.Data;

public static class FixtureReader
{
    public static string ReadRaw(string path)
    {
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Test fixture was not found.", fullPath);
        }

        return File.ReadAllText(fullPath);
    }

    public static T Read<T>(string path)
    {
        return AtfJsonOptions.DeserializeCaseInsensitive<T>(ReadRaw(path));
    }
}
