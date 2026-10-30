# SourceGauge

[![CI](https://github.com/csteinmueller/SourceGauge/actions/workflows/ci.yml/badge.svg)](https://github.com/csteinmueller/SourceGauge/actions/workflows/ci.yml)
[![CodeQL](https://github.com/csteinmueller/SourceGauge/actions/workflows/codeql.yml/badge.svg)](https://github.com/csteinmueller/SourceGauge/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Roslyn-powered code metrics for .NET, built for downstream tools.

SourceGauge is a command-line tool for analyzing C#/.NET codebases and extracting software metrics into structured output for third-party tools. Roslyn is the first analysis backend. SourceGauge reports measurements; it does not enforce rules, emit threshold violations, or fail builds.

> **Status:** early development. No metrics shipped yet. See the [vision](docs/vision.md).

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
tests/SourceGauge.Analyzers.Tests/ tests for the current prototype
docs/                              vision, requirements, ADRs
.github/                           CI, templates, agents, prompts, agent hooks
.githooks/                         local git hooks
scripts/                           setup and hook scripts
```

## License

[MIT](LICENSE)
