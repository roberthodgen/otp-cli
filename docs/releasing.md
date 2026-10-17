# Releasing

## Automated Releases

The release workflow runs when a tag matching `v*` is pushed. The package version comes from the `version` field in root `package.json`, and the tag must match it with a leading `v`. For example, a `package.json` version of `1.2.3` must be released from tag `v1.2.3`.

Create and push a release tag:

```bash
npm version patch
git push --follow-tags
```

The release workflow will:

- Read the package version from `package.json` and fail if the tag does not match `v<version>`.
- Restore, build, and test the solution.
- Pack `src/Client` with the `package.json` version and build Windows and Linux CLI release artifacts under `build/`.
- Build, sign, zip, and notarize macOS CLI binaries on a macOS runner.
- Create a GitHub Release with `gh release create` using generated release notes.
- Attach the NuGet and CLI artifacts to the GitHub Release.

The CLI artifacts attached to each GitHub Release are:

- `otp_win-x64.exe`
- `otp_linux-x64`
- `otp_macos-x64.zip`
- `otp_macos-arm64.zip`

See [Apple Signing and Notarization](apple-signing-notarization.md) for the Apple Developer setup, GitHub Secrets, and verification commands required for macOS releases.

## Release Notes

GitHub generated release notes are configured by `.github/release.yml`. Pull requests with labels are grouped as breaking changes, features, fixes, documentation, maintenance, or other changes.

Use `skip-changelog` on pull requests that should be excluded from generated release notes.
