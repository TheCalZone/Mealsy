// The only platform replacement: production data code uses an isolated temp folder.
internal static class FileSystem
{
    public static string AppDataDirectory { get; } = CreateDirectory();
    private static string CreateDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "mealsy-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(path);
        return path;
    }
}

// Device preferences are replaced with an in-memory dictionary for store integration tests.
internal static class Preferences
{
    private static readonly Dictionary<string, string> values = new();
    public static string Get(string key, string fallback) => values.TryGetValue(key, out var value) ? value : fallback;
    public static void Set(string key, string value) => values[key] = value;
    public static void Remove(string key) => values.Remove(key);
    public static void Clear() => values.Clear();
}
