# OTP Command Implementation Design

## Intent

Turn the `otp` CLI scaffold into a small, testable TOTP credential utility.
Users can persist TOTP provisioning URIs by friendly label, list those labels,
read a current token, copy a current token, and remove entries. Storage must be
portable, deterministic in tests, and overrideable for process-level tests.

## Scope

This increment implements the existing `list`, `create`, `read`, `copy`, and
`delete` command contracts, plus the supporting time, URI/TOTP, persistence,
clipboard, and test infrastructure.

- TOTP is the only supported OTP type.
- Supported algorithms are SHA-1, SHA-256, and SHA-512.
- Defaults for an explicitly constructed credential are SHA-1, a 30-second
  period, and six digits.
- The TextCopy 6.2.1 NuGet package provides the production cross-platform
  clipboard adapter.
- Persistence is plain UTF-8 text containing a canonical `otpauth://` URI.
- `OTP_CLI_OVERRIDE_DIR` isolates storage for process-level tests.

HOTP, encrypted-at-rest storage, import/export, bulk operations beyond the
existing multi-label delete syntax, and a process-level clipboard test seam are
outside this increment.

## Architecture

The application stays in the existing `src/Cli` executable project. Logical
boundaries are expressed with focused types and interfaces rather than new
projects.

### Time

`OtpTimestamp` is an immutable UTC value object around `DateTime`. Its factory
normalizes every supplied value with `ToUniversalTime()` so all code
calculations use a single timescale.

`ISystemClock` exposes an `OtpTimestamp Now` property.

- `RunningSystemClock` obtains the current UTC timestamp for production.
- `KnownSystemClock.FromTime(...)` creates a fixed clock for deterministic
  tests.

Only `RunningSystemClock` reads the system time. TOTP calculation accepts an
`OtpTimestamp` and therefore has no static-time dependency.

### OTP domain

`OtpLabel` stores the decoded, friendly label shown to and accepted from users.
It converts that label to an RFC 3986 percent-encoded storage filename and
back. It rejects empty labels, `.` and `..`, Windows reserved device-name
stems, and labels ending in a period or space. This keeps encoded filenames
portable and ensures a label cannot escape the repository root.

`TotpCredential` holds validated TOTP settings: friendly label, Base32 secret,
algorithm, digit count, and period. An `otpauth://totp/...` parser validates
the URI scheme and type, parses the raw label before percent-decoding, requires
one secret, rejects duplicate known parameters, supports the documented
algorithms and digits, and rejects unsupported or malformed configurations.
Unknown extension parameters are ignored. The serializer produces the canonical
URI that is stored on disk: a lowercase `otpauth://totp/` prefix, an RFC 3986
percent-encoded friendly label, and known query parameters ordered as `secret`,
`algorithm`, `digits`, `period`, and `issuer`. It uppercases secrets and emits
only non-default algorithm, digit, and period parameters; an issuer is emitted
when supplied.

`TotpGenerator` uses .NET HMAC primitives and RFC 6238 dynamic truncation to
generate a token for a credential and timestamp. It supports the three
algorithms above and six or eight digits.

### Persistence

`IOtpRepository` provides four operations:

- list decoded labels;
- get the saved URI for a friendly label;
- create a URI only when its label does not already exist;
- delete a label when it exists.

`FileOtpRepository` maps a label to `<root>/<rfc3986-encoded-label>`. The
entry contents are the canonical full URI encoded as UTF-8. Listing decodes
filenames and returns ordinally sorted labels.

The root resolver uses the non-empty `OTP_CLI_OVERRIDE_DIR` value literally
when present. Otherwise it constructs `~/.config/otp` from the current user's
home directory. The repository creates the root directory only during create.
A missing root lists as empty, reads as missing, and deletes as an idempotent
no-op.

### Clipboard

`IOtpClipboard.Copy(string text)` isolates the pasteboard dependency.
`TextCopyOtpClipboard` implements it by calling TextCopy. Clipboard exceptions
are translated into command failures without exposing the copied OTP in a
diagnostic. Unit tests use a fake clipboard.

### Command composition

A small application dependency bundle supplies repository, clock, clipboard,
and text writers. `Program.cs` uses the production bundle; command and handler
tests inject fakes and temporary repositories. The existing convenience
`CliApplication.CreateRootCommand()` remains available for production startup.

Command handlers orchestrate these interfaces only; they do not manipulate
paths, access the current time, or call TextCopy directly.

## Command Contract

All successful commands return zero. Validation and operational errors return
one and emit a concise, non-secret diagnostic to standard error. Parse errors
continue to be provided by System.CommandLine.

### `otp create <label-or-url> [--secret] [--period] [--algorithm] [--issuer]`

If `label-or-url` is an `otpauth://` URI, the command validates and canonicalizes
it, derives its decoded friendly label from the URI label, and saves it only if
the label is new. The URI must be TOTP.

Otherwise, `--secret` is required. The command builds a canonical TOTP URI from
the supplied label and options. The supplied label remains the stored/displayed
label even when `--issuer` is specified; `--issuer` is written only as the URI
issuer parameter. Absent optional values use the standard defaults.

An existing label leaves the old file unchanged and fails. Malformed URIs,
unsupported types, invalid Base32 secrets or option values, a plain label with
no secret, and combining a URI with creation options fail. Success is silent.

### `otp list`

The command prints `total <n>` followed by one friendly decoded label per line,
in ordinal sort order. A missing repository root is an empty repository and
prints `total 0`. Success is silent apart from this standard output.

### `otp read <label>`

The command retrieves and validates the entry, generates a token using the
injected clock, and prints only the digits plus a newline. If the storage root
or label does not exist, it returns one and writes exactly
`no OTP with that label was found` to standard error. It produces the same
failure shape for invalid stored credentials without including their raw URI or
secret.

### `otp copy <label>`

The command follows the read flow and passes the generated digits to
`IOtpClipboard`. It is silent on success. Missing, malformed, and clipboard
failures write a concise non-secret diagnostic to standard error and return one.

### `otp delete <labels...>`

Each supplied friendly label is deleted when present. A missing entry or root
is a successful no-op: the requested final state has been reached. The command
is silent on success. A real filesystem I/O error reports the affected label
and returns one.

## Test Strategy

Development is test-first: introduce a failing test for one behavior, add the
smallest production change that passes it, then refactor while keeping the
suite green.

### Unit and component tests

- Test UTC timestamp normalization, running/fixed clocks, and fixed-clock
  factories.
- Test TOTP against RFC 6238 vectors for SHA-1, SHA-256, and SHA-512, including
  period boundaries and six/eight-digit output.
- Test URI parse/build behavior, friendly labels such as `example.com:rob`,
  issuer parameter behavior, canonical serialization, and invalid inputs.
- Test file persistence in unique temporary directories: encoded portable
  filenames, UTF-8 contents, duplicate protection, decoded/sorted listing,
  absent-root behavior, deletion idempotence, and I/O errors.
- Test handlers with fakes for the clock and clipboard, asserting standard
  output, standard error, exit codes, and the token passed to the clipboard.

### Process-level tests

The test project launches the built `otp.dll` through `dotnet`, captures stdout,
stderr, and exit code, and sets a unique `OTP_CLI_OVERRIDE_DIR` for each child
process. Helpers seed the override root with raw URI files when needed.

The suite covers the TODO scenarios for an empty list, create from a known URI,
list with one entry, read of a known entry, and delete of a known entry. It also
checks the corresponding storage effects. Process tests do not assert an exact
current TOTP because the child process has a running clock; exact algorithm
values are covered by fixed-clock unit tests.

`copy` is deliberately unit/component tested only. It has no test-only
environment-variable sink and no process-level test because a real desktop
clipboard is unreliable in headless CI. The TODO copy E2E item remains
unchecked in this increment.

## Safety and privacy

Provisioning URIs and TOTP secrets are sensitive. They are stored only in the
label-addressed files, never written to normal command output or diagnostics,
and never embedded in assertion failure messages deliberately produced by the
application. Tests use published dummy Base32 values and unique temporary
directories.
