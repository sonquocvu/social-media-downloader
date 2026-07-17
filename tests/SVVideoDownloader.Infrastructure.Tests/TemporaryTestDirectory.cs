namespace SVVideoDownloader.Infrastructure.Tests;

internal sealed class TemporaryTestDirectory : IDisposable
{
    private static readonly string TestRoot = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "SVVideoDownloader.Tests");

    public TemporaryTestDirectory()
    {
        Directory.CreateDirectory(TestRoot);
        Path = System.IO.Path.Combine(TestRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        var fullPath = System.IO.Path.GetFullPath(Path);
        var fullRoot = System.IO.Path.TrimEndingDirectorySeparator(
            System.IO.Path.GetFullPath(TestRoot)) + System.IO.Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Test cleanup path escaped its root.");
        }

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
        }
    }
}
