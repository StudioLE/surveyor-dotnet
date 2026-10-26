using Surveyor.Utils.CommandLine;

namespace Surveyor.Core.Tests.Resources;

/// <summary>
/// A git repository in a new temporary directory that is deleted on dispose.
/// </summary>
internal sealed class TemporaryGitRepository : IDisposable
{
    private const string Config = "-c user.name=Test -c user.email=test@example.com -c commit.gpgsign=false -c tag.gpgsign=false";

    /// <summary>
    /// The absolute path of the repository.
    /// </summary>
    public string RootDirectory { get; }

    /// <summary>
    /// Create a new repository with <paramref name="branch"/> checked out.
    /// </summary>
    public TemporaryGitRepository(string branch)
    {
        RootDirectory = Path.Combine(Path.GetTempPath(), $"surveyor-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(RootDirectory);
        Git($"init -q -b \"{branch}\"");
    }

    /// <summary>
    /// Commit a new file at <paramref name="relativePath"/>.
    /// </summary>
    public void Commit(string relativePath)
    {
        string path = Path.Combine(RootDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Guid.NewGuid().ToString());
        Git($"add \"{relativePath}\"");
        Git($"commit -q -m \"chore: Add {relativePath}\"");
    }

    /// <summary>
    /// Tag the current commit.
    /// </summary>
    public void Tag(string tag)
    {
        Git($"tag \"{tag}\"");
    }

    private void Git(string arguments)
    {
        Cli.ExecuteOrThrow("git", $"-C \"{RootDirectory}\" {Config} {arguments}");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (string file in Directory.EnumerateFiles(RootDirectory, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(RootDirectory, true);
    }
}
