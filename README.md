# SourceGauge

[![CI](https://github.com/csteinmueller/SourceGauge/actions/workflows/ci.yml/badge.svg)](https://github.com/csteinmueller/SourceGauge/actions/workflows/ci.yml)
[![CodeQL](https://github.com/csteinmueller/SourceGauge/actions/workflows/codeql.yml/badge.svg)](https://github.com/csteinmueller/SourceGauge/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Roslyn-powered code metrics for .NET, built for downstream tools.

SourceGauge is a command-line tool for analyzing C#/.NET codebases and extracting software metrics into structured output for third-party tools. Roslyn is the first analysis backend. SourceGauge reports measurements; it does not enforce rules, emit threshold violations, or fail builds.

> **Status:** early development. No metrics shipped yet. See the [vision](docs/vision.md).

## CLI

The CLI loads C# projects and Visual Studio solution files using the installed .NET SDK and MSBuild. Run it from a .NET 10 SDK environment:

```bash
dotnet run --project src/SourceGauge.Cli -- path/to/project.csproj
dotnet run --project src/SourceGauge.Cli -- path/to/solution.sln
dotnet run --project src/SourceGauge.Cli -- --help
dotnet run --project src/SourceGauge.Cli -- --version
```

Only analyze projects you trust. MSBuild project evaluation may execute project-defined logic; SourceGauge does not sandbox it. During analysis, progress and diagnostics are written to stderr. Standard output is reserved for machine-readable metrics and remains empty until metric export is implemented. Help and version are written to stdout.

Exit codes are `0` for a successful load, help, or version; `1` for an analysis/load failure; and `2` for invalid arguments or input paths.

## Getting started (contributors)

```bash
./scripts/setup.sh          # or: pwsh ./scripts/setup.ps1
dotnet build -c Release
dotnet test -c Release
```

## Documentation

- [Vision](docs/vision.md)
- [Requirements](docs/requirements/README.md)
- [Architecture Decision Records](docs/adr/README.md)
- [Contributing](CONTRIBUTING.md) · [Agent rules](AGENTS.md) · [Security](SECURITY.md)

## Repository layout

```
src/SourceGauge.Analyzers/         current analyzer prototype (netstandard2.0)
src/SourceGauge.Cli/                CLI for loading C# projects and solutions (net10.0)
tests/SourceGauge.Analyzers.Tests/ tests for the current prototype
tests/SourceGauge.Cli.Tests/       CLI command and loading tests
docs/                              vision, requirements, ADRs
.github/                           CI, templates, agents, prompts, agent hooks
.githooks/                         local git hooks
scripts/                           setup and hook scripts
```

## License

[MIT](LICENSE)
