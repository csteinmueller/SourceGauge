# SourceGauge

[![CI](https://github.com/csteinmueller/SourceGauge/actions/workflows/ci.yml/badge.svg)](https://github.com/csteinmueller/SourceGauge/actions/workflows/ci.yml)
[![CodeQL](https://github.com/csteinmueller/SourceGauge/actions/workflows/codeql.yml/badge.svg)](https://github.com/csteinmueller/SourceGauge/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Roslyn-powered code metrics for .NET, built for downstream tools.

SourceGauge is a command-line tool for analyzing C#/.NET codebases and extracting software metrics into structured output for third-party tools. Roslyn is the first analysis backend. SourceGauge reports measurements; it does not enforce rules, emit threshold violations, or fail builds.

> **Status:** early development. Method-level FanIn is the first metric; packaging and release are tracked separately. See the [FanIn definition](docs/adr/0008-fanin-metric-definition.md) and [vision](docs/vision.md).

## CLI

The CLI loads C# projects and Visual Studio solution files using the installed .NET SDK and MSBuild. Run it from a .NET 10 SDK environment:

```bash
dotnet run --project src/SourceGauge.Cli -- path/to/project.csproj
dotnet run --project src/SourceGauge.Cli -- path/to/solution.sln
dotnet run --project src/SourceGauge.Cli -- --output metrics.json path/to/project.csproj
dotnet run --project src/SourceGauge.Cli -- --help
dotnet run --project src/SourceGauge.Cli -- --version
```

Only analyze projects you trust. MSBuild project evaluation may execute project-defined logic; SourceGauge does not sandbox it. Operational messages and diagnostics are written to stderr. A successful analysis writes one versioned JSON document to stdout by default, or to the file selected with `--output`; MVP1 currently reports method-level FanIn, and analyses with no eligible methods emit an empty `metrics` array. The [FanIn definition](docs/adr/0008-fanin-metric-definition.md), [JSON Schema](docs/schema/metric-results-v1.schema.json), and [sample document](docs/samples/metric-results-v1.json) define the v1 behavior and output contract. Help and version are written to stdout.

Exit codes are `0` for a successful load/export, help, or version; `1` for an analysis or export failure; and `2` for invalid arguments or input paths.

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
- [FanIn metric definition](docs/adr/0008-fanin-metric-definition.md)
- [Contributing](CONTRIBUTING.md) · [Agent rules](AGENTS.md) · [Security](SECURITY.md)

## Repository layout

```
src/SourceGauge.Core/               language-neutral metric result model (net10.0)
src/SourceGauge.CSharp/             Roslyn-backed C# metric generation (net10.0)
src/SourceGauge.Cli/                CLI loading, orchestration, and JSON export (net10.0)
tests/SourceGauge.CSharp.Tests/     C# metric collector tests
tests/SourceGauge.Cli.Tests/        CLI command, loading, and export integration tests
docs/                              vision, requirements, ADRs
.github/                           CI, templates, agents, prompts, agent hooks
.githooks/                         local git hooks
scripts/                           setup and hook scripts
```

## License

[MIT](LICENSE)
