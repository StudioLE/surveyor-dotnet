## About

A CLI tool to handle essential CI/CD tasks such as:

### Version Numbering

Determine assembly and package version numbers by analyzing conventional commit messages and determining whether files have changed since the last version released as a git tag.

- Each project, assembly, or package in a solution is versioned independently so new versions are only released if the assembly has changed, as opposed to publishing every package in the repository.
- Analyses commit messages formatted according to the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification to determine whether a version bump should be a patch, minor, or major release.
- Version numbers follow the [Semantic Versioning 2](https://semver.org/spec/v2.0.0.html) specification.
- Version numbers are recorded as git tags ensuring full compatibility with your existing workflow.
- Per branch versioning gives the flexibility to simultaneously develop `v2.0.0` while providing patches for your `v1` branch.
- Pre-release versioning is fully supported via `alpha`, `beta`, and `rc` branches.

### Generating Release Notes

Generate release notes by analyzing conventional commit messages and grouping them by type (breaking changes, features, fixes, etc.).

## How to Use

Install as a [.NET Core Global Tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-use):

```bash
dotnet tool install --global surveyor --prerelease
```

Determine the version number for the current working directory:

```bash
Surveyor version
```

Determine the version number for a repository:

```bash
Surveyor version --Versioning:Directory "/e/Repos/C#/Example/StudioLE.Example.Cli/src"
```

Determine the version number for a specific project:

```bash
Surveyor project-version --Versioning:Package "StudioLE.Example" --Versioning:Directory "/e/Repos/C#/Example/StudioLE.Example.Cli/src"
```

Generate release notes for all changes since the last tagged version of the current working directory:

```bash
Surveyor release-notes
```

Use Surveyor from GitHub Actions:

https://github.com/StudioLE/Actions/blob/7c6aea58c211d6411dc428bfb4b5fb0077884a0e/.github/workflows/02-release.yml#L55-L99


## How it Works

The CLI logic is handled by [Program.cs](Program.cs) which calls activities defined in the [Surveyor.Core](../../Surveyor.Core/src) project. The inputs for the activities are assigned by the [Options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) providing versatility to define the inputs via command line arguments, environment variables, or configuration files.
