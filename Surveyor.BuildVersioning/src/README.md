## About

An MSBuild task to automatically apply assembly and package version numbers by analyzing conventional commit messages and determining whether files have changed since the last version released as a git tag.

Each project, assembly, or package in a solution is versioned independently so new versions are only released if the assembly has changed, as opposed to publishing every package in the repository.

## Key Features

- Each project, assembly, or package in a solution is versioned independently so new versions are only released if the assembly has actually changed, as opposed to
- Analyses commit messages formatted according to the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification to determine whether a version bump should be a patch, minor, or major release.
- Version numbers follow the [Semantic Versioning 2](https://semver.org/spec/v2.0.0.html) specification.
- Version numbers are recorded as git tags ensuring full compatibility with your existing workflow.
- Per branch versioning gives the flexibility to simultaneously develop `v2.0.0` while providing patches for your `v1` branch.
- Pre-release versioning is fully supported via `alpha`, `beta`, and `rc` branches.

## How to Use

Add the `Surveyor.BuildVersioning` NuGet package to your project:

```xml
<PackageReference Include="Surveyor.BuildVersioning" Version="0.4.0" PrivateAssets="All" Condition="'$(CI)' == 'true'" />
```

Optionally, configure it to only run in your CI build environment, or for `Release` builds:

```xml
<PackageReference Include="Surveyor.BuildVersioning" Version="0.4.0" PrivateAssets="All" Condition="'$(CI)' == 'true'" />
```

```xml
<PackageReference Include="Surveyor.BuildVersioning" Version="0.4.0" PrivateAssets="All" Condition="'$(Configuration)' == 'Release'" />
```

## How it Works

The versioning logic is executed by [VersioningTask](VersioningTask.cs) which is implemented as a [custom MSBuild task](https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-custom-task-code-generation). The task itself calls the [ProjectVersioningActivity](../../Surveyor.Core/src/Versioning/ProjectVersioningActivity.cs).

When the `Surveyor.BuildVersioning` package is included as a `PackageReference` the MSBuild process automatically calls [Surveyor.BuildVersioning.props](tools/Surveyor.BuildVersioning.props) which adds the task, and [Surveyor.BuildVersioning.targets](tools/Surveyor.BuildVersioning.targets) which executes the task and assigns the resulting properties.
