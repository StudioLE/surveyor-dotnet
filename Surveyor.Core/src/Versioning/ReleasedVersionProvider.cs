using Surveyor.System;
using Surveyor.VersionControl;

namespace Surveyor.Versioning;

/// <summary>
/// A strategy to provide the version a project was last released at.
/// </summary>
/// <remarks>
/// - Uses the earliest tag that includes the latest change to the project directory.
/// </remarks>
public interface IReleasedVersionProvider
{
    /// <summary>
    /// Get the earliest tagged version that includes the latest change to <paramref name="projectDirectory"/>.
    /// </summary>
    /// <param name="projectDirectory">The path of the project directory.</param>
    /// <param name="branchName">The name of the branch.</param>
    /// <param name="releaseStream">The release stream of the branch.</param>
    /// <returns>
    /// The version, or <see langword="null"/> if no tag includes the latest change.
    /// </returns>
    public SemanticVersion? Get(string projectDirectory, string branchName, ReleaseStream releaseStream);
}

/// <inheritdoc/>
public class ReleasedVersionProvider : IReleasedVersionProvider
{
    private readonly GitCli _git;

    /// <summary>
    /// DI constructor for <see cref="ReleasedVersionProvider"/>.
    /// </summary>
    public ReleasedVersionProvider(GitCli git)
    {
        _git = git;
    }

    /// <inheritdoc/>
    public SemanticVersion? Get(string projectDirectory, string branchName, ReleaseStream releaseStream)
    {
        string relativeProjectDirectory = Path
            .GetRelativePath(_git.RootDirectory, Path.GetFullPath(projectDirectory))
            .Replace('\\', '/');
        string? commit = _git.GetLastCommit(relativeProjectDirectory, branchName);
        if (commit is null)
            return null;
        return _git
            .GetTagsContaining(commit, branchName)
            .Where(x => x.StartsWith("v"))
            .Select(SemanticVersion.Create)
            .OfType<SemanticVersion>()
            .Where(x => releaseStream.IsPreRelease || !x.IsPreRelease())
            .OrderBy(x => x)
            .FirstOrNull();
    }
}
