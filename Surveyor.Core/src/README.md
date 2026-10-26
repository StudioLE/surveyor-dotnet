## About

Core libraries for the [Surveyor.BuildVersioning](../../Surveyor.BuildVersioning/src) MSBuild task and [Surveyor.Cli](../../Surveyor.Cli/src) CLI tool providing logic for:

### Versioning Activities

Determine assembly and package version numbers by analyzing conventional commit messages and determining whether files have changed since the last version released as a git tag.

Each project, assembly, or package in a solution is versioned independently so new versions are only released if the assembly has changed, as opposed to publishing every package in the repository.

- [ProjectVersioningActivity](Versioning/ProjectVersioningActivity.cs)
- [RepositoryVersioningActivity](Versioning/RepositoryVersioningActivity.cs)

### Release Note Generation Activity

Generate release notes by analyzing conventional commit messages and grouping them by type (breaking changes, features, fixes, etc.).

- [ReleaseNotesActivity](ReleaseNotes/ReleaseNotesActivity.cs)

### Conventional Commit Parsing
- [ConventionalCommitParser](VersionControl/ConventionalCommitParser.cs)

### Git CLI methods
- [GitCli](VersionControl/GitCli.cs)

### Standards

- Follows the [Dependency injection (DI) software design pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) ensuring ease of extensibility and adaptation to your use case.
- Project structure and code standards follow the [StudioLE Guidelines](https://github.com/StudioLE/Example).


## How to Use

- The [unit tests](../tests) provide clear examples of how to use the core libraries.
- [Surveyor.Cli](../../Surveyor.Cli/src) provides a CLI executable implementation.
- [Surveyor.BuildVersioning](../../Surveyor.BuildVersioning/src) provides an MSBuild task implementation.
