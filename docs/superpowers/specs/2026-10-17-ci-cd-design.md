# CI/CD Design

## Intent

Add GitHub Actions automation that verifies every pull request and turns a
validated version tag into a GitHub release containing cross-platform OTP CLI
artifacts. The workflow must use the repository's existing .NET 10 solution,
release documentation, and macOS signing/notarization requirements.

## Scope

- Run CI for pull requests targeting any branch.
- Build the solution before testing the currently available test suite.
- Prepare the PR workflow for a future, distinct end-to-end test step without
  adding a misleading no-op step today.
- Release on pushed `v*` tags only when the tag matches the root
  `package.json` version.
- Publish Windows x64, Linux x64, macOS x64, and macOS arm64 CLI artifacts to
  a generated-notes GitHub release.
- Sign and notarize the macOS ZIP artifacts using the documented `release`
  environment secrets.
- Configure generated GitHub release notes and align release documentation with
  the CLI-only process.

NuGet package creation and publication are explicitly out of scope.

## Pull Request Workflow

`.github/workflows/pr.yml` defines one Ubuntu job triggered by pull-request
`opened`, `reopened`, and `synchronize` events without a base-branch filter.
It therefore verifies pull requests for every branch.

The job uses .NET 10 and performs these named steps in order:

1. Checkout source.
2. Restore `otp-cli.slnx`.
3. Build `otp-cli.slnx` in Release configuration without restoring again.
4. Run the current solution tests in Release configuration without rebuilding
   or restoring again.

The final test step exercises the existing unit-test project. A future E2E
project receives a separate named step after the build; no placeholder E2E
step is added until that suite exists.

## Release Workflow

`.github/workflows/release.yml` is triggered by pushes of tags matching `v*`.
It grants `contents: write` so its final job can create a GitHub release.

The workflow reads `package.json` with Node, obtains the `version` field, and
fails unless `GITHUB_REF_NAME` equals `v<version>`. The matching package version
is passed to CLI publishes so release binaries carry the release version.

### Ubuntu build job

The `build` job runs on `ubuntu-latest`. It restores, builds, and tests the
solution in Release configuration, then publishes framework-dependent Windows
x64 and Linux x64 CLI binaries into a `build` directory:

- `otp_win-x64.exe`
- `otp_linux-x64`

It uploads these files as a workflow artifact. No `dotnet pack` or NuGet push
is performed.

### macOS job

The `macos` job runs on `macos-latest` and uses the protected `release`
environment. It publishes native-AOT macOS x64 and arm64 binaries, imports the
Developer ID Application P12 into a temporary keychain, and signs each raw
executable with hardened runtime and a timestamp.

Each signed executable is verified, placed in an architecture-specific ZIP,
and submitted to Apple Notary Service with `xcrun notarytool --wait`. The
accepted, notarized ZIP artifacts are:

- `otp_macos-x64.zip`
- `otp_macos-arm64.zip`

ZIP artifacts are deliberately not stapled: as documented, Gatekeeper obtains
their notarization ticket online when users extract and run the executable.
The job requires the existing documented environment secrets:

- `APPLE_CERTIFICATE_P12_BASE64`
- `APPLE_CERTIFICATE_PASSWORD`
- `APPLE_KEYCHAIN_PASSWORD`
- `APPLE_CODESIGN_IDENTITY`
- `APPLE_ID`
- `APPLE_TEAM_ID`
- `APPLE_APP_SPECIFIC_PASSWORD`

### Publish job

The `publish` job waits for both platform build jobs and uses the `release`
environment. It downloads the four artifacts and executes `gh release create`
for the pushed tag with generated notes, a tag-based title, and every artifact
attached. It does not publish to NuGet or another package registry.

## Generated Release Notes

`.github/release.yml` configures GitHub-generated release notes. It excludes
PRs labelled `skip-changelog`, `duplicate`, `invalid`, or `wontfix`, and
groups the remaining PRs in this order:

1. Breaking Changes (`breaking-change`, `breaking`)
2. Features (`feature`, `enhancement`)
3. Fixes (`bug`, `fix`)
4. Documentation (`documentation`, `docs`)
5. Maintenance (`dependencies`, `maintenance`, `chore`, `ci`)
6. Other Changes (catch-all)

## Documentation

`docs/releasing.md` will describe a CLI-only release: tag/package-version
validation, solution verification, four CLI artifacts, and the GitHub release.
`docs/apple-signing-notarization.md` will describe the three actual jobs and
remove stale NuGet packaging/publishing references. The latter remains the
source of truth for certificate, secret, signing, notarization, and local
verification requirements.

## Failure Behavior

- A restore, build, or test failure fails the associated GitHub Actions job.
- A tag/version mismatch fails before platform artifacts or a release are
  produced.
- Missing macOS secrets, certificate import/signing errors, or a rejected
  notarization fail the macOS job and prevent publication.
- The GitHub release is created only after every required artifact build has
  succeeded.

## Verification

Local verification runs `make build` and `make test`. CI validates the PR
workflow against the same solution. GitHub-hosted release execution is required
to validate protected-environment access, Apple signing/notarization, artifact
transfer, and creation of an actual GitHub release.
