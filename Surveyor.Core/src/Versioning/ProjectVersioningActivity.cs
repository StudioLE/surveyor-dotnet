using Microsoft.Extensions.Logging;
using Surveyor.VersionControl;

namespace Surveyor.Versioning;

/// <summary>
/// Automatically determine the version of the package by:
/// <para>Finding the earliest tagged version that includes the latest change to the project directory</para>
/// <para>Otherwise, using the version <see cref="RepositoryVersioningActivity"/> determines for the repository</para>
/// </summary>
public class ProjectVersioningActivity
{
    private readonly ILogger<ProjectVersioningActivity> _logger;
    private readonly GitCli _git;
    private readonly IReleasedVersionProvider _releasedVersionProvider;
    private readonly IReleaseStreamProvider _releaseStreamProvider;
    private readonly RepositoryVersioningActivity _repositoryVersioningActivity;

    /// <summary>
    /// DI constructor for <see cref="ProjectVersioningActivity"/>.
    /// </summary>
    public ProjectVersioningActivity(
        ILogger<ProjectVersioningActivity> logger,
        GitCli git,
        IReleasedVersionProvider releasedVersionProvider,
        IReleaseStreamProvider releaseStreamProvider,
        RepositoryVersioningActivity repositoryVersioningActivity)
    {
        _logger = logger;
        _git = git;
        _releasedVersionProvider = releasedVersionProvider;
        _releaseStreamProvider = releaseStreamProvider;
        _repositoryVersioningActivity = repositoryVersioningActivity;
    }

    /// <summary>
    /// Determine the version of the package.
    /// </summary>
    /// <returns>
    /// The version, or <see langword="null"/> if the options are invalid or the branch is not a release stream.
    /// </returns>
    public Task<SemanticVersion?> Execute(VersioningActivityOptions options)
    {
        return Task.FromResult(ExecuteInternal(options));
    }

    private SemanticVersion? ExecuteInternal(VersioningActivityOptions options)
    {
        if (string.IsNullOrEmpty(options.Package))
        {
            _logger.LogError($"[{options.Package}] The {nameof(options.Package)} option is required.");
            return null;
        }
        if (string.IsNullOrEmpty(options.Directory))
        {
            _logger.LogError($"[{options.Package}] The {nameof(options.Directory)} option is required.");
            return null;
        }
        if (!Directory.Exists(options.Directory))
        {
            _logger.LogError($"[{options.Package}] The {nameof(options.Directory)} does not exist: {options.Directory}.");
            return null;
        }
        if(_git.RootDirectory != options.Directory)
            _git.SetRootDirectory(options.Directory);
        if (string.IsNullOrEmpty(options.Branch))
            options.Branch = _git.GetCurrentBranch();
        ReleaseStream? releaseStreamQuery = _releaseStreamProvider.Get(options.Branch);
        if (releaseStreamQuery is not ReleaseStream releaseStream)
        {
            _logger.LogInformation($"[{options.Package}] The {options.Branch} branch is not a release stream.");
            return null;
        }
        SemanticVersion? releasedVersion = _releasedVersionProvider.Get(options.Directory, options.Branch, releaseStream);
        if (releasedVersion is not null)
        {
            _logger.LogInformation($"[{options.Package}] No changes have been made since the last released version: {releasedVersion}.");
            return releasedVersion;
        }
        _logger.LogDebug($"[{options.Package}] Changes have been made since the last released version.");
        return _repositoryVersioningActivity.Execute(new()
        {
            Branch = options.Branch,
            Directory = options.Directory
        });
    }
}
