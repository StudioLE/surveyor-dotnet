using Surveyor.Versioning;

namespace Surveyor.Core.Tests.Resources;

public class MockReleaseTypeStrategy : IReleaseTypeStrategy
{
    private readonly ReleaseType _releaseType;
    private readonly IReadOnlyDictionary<SemanticVersion, ReleaseType> _releaseTypesSince;

    public MockReleaseTypeStrategy(ReleaseType releaseType)
        : this(releaseType, new Dictionary<SemanticVersion, ReleaseType>())
    {
    }

    /// <param name="releaseType">The release type when <paramref name="releaseTypesSince"/> has no entry.</param>
    /// <param name="releaseTypesSince">The release type of the commits since each version.</param>
    public MockReleaseTypeStrategy(ReleaseType releaseType, IReadOnlyDictionary<SemanticVersion, ReleaseType> releaseTypesSince)
    {
        _releaseType = releaseType;
        _releaseTypesSince = releaseTypesSince;
    }

    /// <inheritdoc />
    public ReleaseType Get()
    {
        return _releaseType;
    }

    /// <inheritdoc/>
    public ReleaseType Get(SemanticVersion latestVersion)
    {
        return _releaseTypesSince.TryGetValue(latestVersion, out ReleaseType releaseType)
            ? releaseType
            : _releaseType;
    }
}
