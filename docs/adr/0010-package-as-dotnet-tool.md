# ADR-0010: Package SourceGauge as a .NET Tool

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** @csteinmueller
- **Related:** #20, ADR-0004, ADR-0005, ADR-0007, ADR-0009

## Context

Users need a repeatable installation and upgrade path for the SourceGauge CLI. The current CLI targets `net10.0`, loads project and solution inputs through the installed MSBuild toolset, and uses `VersionPrefix` in `Directory.Build.props`. The repository does not yet package the CLI for installation.

The distribution method must preserve the CLI, CSharp, and Core boundaries in ADR-0009; accurately communicate the .NET SDK/MSBuild prerequisite and trusted-input limitation in ADR-0005; and keep the metric JSON schema version independent of the tool package version.

## Options considered

1. **Framework-dependent NuGet .NET tool** — Reuses the .NET SDK's install, local-manifest, update, and uninstall workflows and keeps one package cross-platform. Users must install a compatible .NET 10 SDK/MSBuild toolset separately.
2. **Self-contained platform-specific binaries** — Reduces the runtime prerequisite but requires platform-specific packages and release validation for every supported runtime identifier.
3. **Container distribution** — Provides a controlled runtime but adds container setup and source-mounting friction to a local source-analysis CLI.
4. **Hosted service** — Removes local installation but conflicts with the local CLI workflow and is outside the product vision.

## Decision

We will package the CLI as one framework-dependent .NET tool from `SourceGauge.Cli`. Use NuGet package ID `SourceGauge.Tool` and command name `sourcegauge`; verify package ID availability and ownership before the first publication, selecting an alternate ID before release if unavailable. Keep `SourceGauge.Core` and `SourceGauge.CSharp` as implementation dependencies, not separate public tool packages.

The tool targets `net10.0`. Installing the tool does not install the .NET SDK or MSBuild. Users need a compatible installed .NET 10 SDK/MSBuild toolset to analyze C# projects and solutions, and must only analyze trusted inputs because MSBuild evaluation can execute project-defined logic. User-facing install/use documentation must state both prerequisites and the trusted-input warning.

Initially claim support only for Windows x64 and Linux x64, matching the current hosted CI runner architectures. Do not claim macOS, ARM, or other runtime support until package installation and end-to-end analysis are added to CI for those targets.

Use `VersionPrefix` in `Directory.Build.props` as the explicit package version source. Do not introduce commit-derived automatic versioning in this decision. Release tags use `vX.Y.Z` and must match the package version exactly. The CLI's reported tool version comes from that package/build version; JSON `schemaVersion` remains governed independently by ADR-0007.

Support both local and global .NET tool installation. Commit a local tool manifest pinned to an exact stable package version for repository use. Because issue #20 does not publish a package, CI must bootstrap that manifest from a package built in the same run: pack the CLI to a local feed first, then restore the manifest using that feed. After the first approved publication, the same pinned manifest can restore from NuGet.org. Document local install, pinned update, and uninstall, plus optional global install, update, and uninstall. Do not use floating versions in the repository manifest.

The package smoke test runs on every claimed platform using a locally built package and an isolated temporary tool manifest. It verifies package metadata and contents, installation, `sourcegauge --version`, analysis of the representative fixture, valid versioned JSON output, and uninstall. It must also ensure the tool package does not ship an SDK-provided `Microsoft.Build.Framework` runtime assembly that conflicts with the registered SDK.

Release automation is tag-triggered and validates that the tag matches `VersionPrefix`, then restores solution dependencies, format-checks, builds, tests, and packs the CLI. After packing, it restores the pinned tool manifest from the local feed and runs the package smoke test before publication. Publish to NuGet.org in a separate job using a NuGet API key supplied as a protected environment secret; do not treat GitHub's `packages: write` permission as authorization to publish to NuGet.org. The publish job must remain disabled until a GitHub environment with required reviewers is configured and verified by a maintainer. Merely naming an environment does not establish an approval gate. Pull request workflows never publish. Configuring GitHub environments or other repository settings is a separate maintainer action and is not included in this issue; no package is published by issue #20.

## Consequences

- Users get standard .NET tool installation and version-pinning workflows without adding a SourceGauge reference to analyzed projects.
- Users must separately install the .NET 10 SDK/MSBuild toolset; tool installation alone is not a self-contained runtime.
- Initial support is limited to tested Windows x64 and Linux x64 environments.
- `VersionPrefix`, matching `vX.Y.Z` tags, and the package's reported tool version must remain aligned; JSON schema compatibility remains independent.
- Local-package smoke tests protect the package entry point and runtime behavior before any publication.
- Before the first publication, CI must build a local package feed to bootstrap the exact-version repository tool manifest.
- Publication remains a human-approved release action; issue #20 does not authorize publishing packages or changing repository settings.