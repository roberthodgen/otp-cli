# Apple Signing and Notarization

This project distributes macOS CLI releases as notarized zip files:

- `otp_macos-x64.zip`
- `otp_macos-arm64.zip`

The release workflow builds the macOS binaries on `macos-latest`, signs each executable with a Developer ID Application certificate, creates one zip per architecture, submits each zip to Apple notarization, and attaches the notarized zip files to the GitHub Release.

## Apple Developer Requirements

You need an active Apple Developer Program membership and access to the Apple Developer team used for signing releases.

Create or use an existing certificate with this type:

- `Developer ID Application`

The signing identity name stored in GitHub must match the certificate identity reported by `security find-identity`, for example:

```text
Developer ID Application: Your Name or Organization (TEAMID)
```

## Export the Certificate

On a trusted Mac with the Developer ID Application certificate installed:

1. Open Keychain Access.
2. Select the Developer ID Application certificate and its private key.
3. Export them as a `.p12` file.
4. Set a strong export password. This becomes `APPLE_CERTIFICATE_PASSWORD`.

Convert the `.p12` to base64 for GitHub Secrets:

```bash
base64 -i DeveloperIDApplication.p12 | pbcopy
```

Paste the copied value into `APPLE_CERTIFICATE_P12_BASE64`.

## GitHub Secrets

Add these secrets under Repository Settings -> Secrets and variables -> Actions:

| Secret | Value |
| --- | --- |
| `APPLE_CERTIFICATE_P12_BASE64` | Base64-encoded `.p12` containing the Developer ID Application certificate and private key. |
| `APPLE_CERTIFICATE_PASSWORD` | Password used when exporting the `.p12`. |
| `APPLE_KEYCHAIN_PASSWORD` | Random password used by GitHub Actions for the temporary build keychain. |
| `APPLE_CODESIGN_IDENTITY` | Full Developer ID Application identity, for example `Developer ID Application: Your Name (TEAMID)`. |
| `APPLE_ID` | Apple ID email address used for notarization. |
| `APPLE_TEAM_ID` | Apple Developer Team ID. |
| `APPLE_APP_SPECIFIC_PASSWORD` | App-specific password for the Apple ID used with `notarytool`. |

Create an app-specific password at https://appleid.apple.com/account/manage in the Sign-In and Security section.

## GitHub Environment

The release workflow uses the `release` environment. Keep Apple signing secrets restricted to that environment when possible.

Recommended environment protection:

- Require approval before deployment.
- Restrict deployment branches or tags to release tags.
- Store Apple signing secrets as environment secrets rather than repository-wide secrets if the repository policy allows it.

## Release Workflow Behavior

On a `v*` tag push, `.github/workflows/release.yml` runs three jobs:

- `build` on Ubuntu restores, tests, packages NuGet artifacts, and builds Windows/Linux CLI binaries.
- `macos` on macOS restores, builds macOS CLI binaries, imports the signing certificate, signs each binary, zips the signed binaries, and submits the zips to Apple notarization.
- `publish` downloads all artifacts, publishes NuGet packages, and creates the GitHub Release.

The macOS job signs the raw executables before zipping them:

```bash
codesign --force --timestamp --options runtime --sign "$APPLE_CODESIGN_IDENTITY" build/otp_macos-x64
codesign --force --timestamp --options runtime --sign "$APPLE_CODESIGN_IDENTITY" build/otp_macos-arm64
```

The job notarizes the zip files:

```bash
xcrun notarytool submit build/otp_macos-x64.zip --apple-id "$APPLE_ID" --team-id "$APPLE_TEAM_ID" --password "$APPLE_APP_SPECIFIC_PASSWORD" --wait
xcrun notarytool submit build/otp_macos-arm64.zip --apple-id "$APPLE_ID" --team-id "$APPLE_TEAM_ID" --password "$APPLE_APP_SPECIFIC_PASSWORD" --wait
```

Zip files are notarized but are not stapled like `.app`, `.pkg`, or `.dmg` files. Gatekeeper validates the notarization ticket online when the user extracts and runs the signed executable.

## Local Verification

After downloading and extracting a macOS release zip on a Mac, verify the executable:

```bash
codesign --verify --strict --verbose=2 otp_macos-arm64
codesign --display --verbose=4 otp_macos-arm64
spctl -a -t exec -vv otp_macos-arm64
```

For x64, replace `otp_macos-arm64` with `otp_macos-x64`.

`spctl` should report that the executable is accepted and identify the Developer ID authority.

## Troubleshooting

If certificate import fails, confirm the `.p12` contains both the certificate and private key and that `APPLE_CERTIFICATE_PASSWORD` matches the export password.

If `codesign` cannot find the identity, compare `APPLE_CODESIGN_IDENTITY` with the output from `security find-identity -v -p codesigning build.keychain` in the workflow logs.

If notarization fails, inspect the `notarytool` output for rejected files or signing issues. The most common causes are an invalid app-specific password, wrong team ID, or a binary that was not signed with a Developer ID Application certificate.
