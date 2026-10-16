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
