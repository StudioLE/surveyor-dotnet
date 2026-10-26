using Surveyor.Versioning;

namespace Surveyor.Core.Tests.Resources;

internal class MockReleasedVersionProvider : IReleasedVersionProvider
{
    private readonly SemanticVersion? _version;

    public MockReleasedVersionProvider(SemanticVersion? version)
    {
        _version = version;
    }

    /// <inheritdoc/>
    public SemanticVersion? Get(string projectDirectory, string branchName, ReleaseStream releaseStream)
    {
        return _version;
    }
}
