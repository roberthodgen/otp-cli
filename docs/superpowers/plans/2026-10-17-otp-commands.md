# OTP Commands Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement persistent, cross-platform TOTP management through the `otp` CLI commands with deterministic unit and isolated process-level tests.

**Architecture:** Keep the existing executable project and add focused time, TOTP, storage, clipboard, and application-service types. Command handlers compose an injected `OtpCommandService`; production composition supplies a running clock, file repository, and TextCopy adapter, while tests substitute deterministic clocks and fakes.

**Tech Stack:** .NET 10, C#, System.CommandLine 2.0.13, xUnit v3, Shouldly, TextCopy 6.2.1, .NET cryptography APIs.

**Spec:** `docs/superpowers/specs/2026-10-17-otp-commands-design.md`

## Global Constraints

- Target `net10.0`; retain the `otp` executable assembly and the existing System.CommandLine command syntax.
- Support TOTP only, with SHA-1/SHA-256/SHA-512, six/eight digits, and defaults of SHA-1, 30 seconds, and six digits.
- Store canonical UTF-8 `otpauth://` URIs in files under `OTP_CLI_OVERRIDE_DIR` when non-empty, otherwise `~/.config/otp`.
- Encode friendly labels as RFC 3986 filenames; never expose a URI or secret in CLI diagnostics.
- Create the storage directory only on create; a missing root lists as empty, reads as not found, and deletes successfully as a no-op.
- Use TextCopy 6.2.1 only behind `IOtpClipboard`; do not add a test-only clipboard environment variable or process-level clipboard test.
- Use TDD: every behavior begins with a failing test, then minimal implementation, focused test verification, and a task-scoped commit.
- Preserve unrelated `.idea/` and `otp-cli.sln.DotSettings.user` working-tree files.

## Review Focus

- An issuer-prefixed URI label such as `Example%20Issuer%3A%20rob` must list and accept the decoded string `Example Issuer: rob`; cover in Task 3 URI tests.
- A label whose RFC 3986 encoding remains a Windows device name (`CON`, `LPT1`, etc.) must fail before filesystem access; cover in Task 2 label tests.
- A duplicate known query parameter with different casing (for example, `secret=A&SECRET=B`) must be rejected; cover in Task 3 URI tests.
- A corrupt persisted URI must fail `read` and `copy` without including the URI or secret in standard error; cover in Task 4 application-service tests.
- If multi-label deletion hits an I/O failure after deleting an earlier label, it must report failure rather than claim total success; cover in Task 4 application-service tests with a throwing repository fake.

---

## File Structure

```text
src/Cli/
  Time/{OtpTimestamp,ISystemClock,RunningSystemClock,KnownSystemClock}.cs
  Otp/{OtpLabel,OtpValidationException,TotpCredential,TotpGenerator}.cs
  Storage/{IOtpRepository,FileOtpRepository}.cs
  Clipboard/{IOtpClipboard,TextCopyOtpClipboard}.cs
  Application/{OtpCommandOutcome,OtpCommandService,CliDependencies}.cs
  CliApplication.cs
  CliExitCodes.cs
  RobertHodgen.Otp.Cli.csproj
src/Tests/Cli/
  Time/ClockTests.cs
  Otp/{OtpLabelTests,TotpCredentialTests,TotpGeneratorTests}.cs
  Storage/FileOtpRepositoryTests.cs
  Application/OtpCommandServiceTests.cs
  EndToEnd/{TemporaryDirectory,CliProcess,OtpCliEndToEndTests}.cs
  CliApplicationTests.cs
TODO.md
```

### Task 1: Add deterministic time primitives

**Files:**
- Create: `src/Cli/Time/OtpTimestamp.cs`, `ISystemClock.cs`, `RunningSystemClock.cs`, `KnownSystemClock.cs`
- Create: `src/Tests/Cli/Time/ClockTests.cs`

**Interfaces:**
- Produces: `public readonly record struct OtpTimestamp(DateTime Value)` with `public static OtpTimestamp FromTime(DateTime value)`; `public interface ISystemClock { OtpTimestamp Now { get; } }`; `public sealed class RunningSystemClock : ISystemClock`; `public sealed class KnownSystemClock : ISystemClock` with `public static KnownSystemClock FromTime(DateTime value)`.
- Consumed by: Task 3 `TotpGenerator` and Task 4 `OtpCommandService`.

- [ ] **Step 1: Write the failing clock tests**

```csharp
[Fact]
public void FromTime_normalizes_values_to_utc()
{
    var timestamp = OtpTimestamp.FromTime(new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc));

    timestamp.Value.Kind.ShouldBe(DateTimeKind.Utc);
    timestamp.Value.ShouldBe(new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc));
}

[Fact]
public void Known_clock_returns_its_fixed_timestamp()
{
    var expected = OtpTimestamp.FromTime(DateTime.UnixEpoch.AddSeconds(59));

    KnownSystemClock.FromTime(expected.Value).Now.ShouldBe(expected);
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~ClockTests"`

Expected: FAIL because the time types do not exist.

- [ ] **Step 3: Implement the time types**

`OtpTimestamp.FromTime` calls `value.ToUniversalTime()`. `RunningSystemClock.Now` wraps `DateTime.UtcNow`; `KnownSystemClock` retains a normalized supplied timestamp.

- [ ] **Step 4: Run the focused test**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~ClockTests"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Cli/Time src/Tests/Cli/Time
git commit -m "feat: add deterministic OTP clock"
```

### Task 2: Add friendly labels and file-backed storage

**Files:**
- Create: `src/Cli/Otp/OtpLabel.cs`, `OtpValidationException.cs`
- Create: `src/Cli/Storage/IOtpRepository.cs`, `FileOtpRepository.cs`
- Create: `src/Tests/Cli/Otp/OtpLabelTests.cs`
- Create: `src/Tests/Cli/Storage/FileOtpRepositoryTests.cs`

**Interfaces:**
- Produces: `public sealed class OtpLabel` with `public static OtpLabel Parse(string value)`, `public string Value { get; }`, and `public string ToStorageFileName()`.
- Produces: `public interface IOtpRepository` with `IReadOnlyList<OtpLabel> List()`, `string? Get(OtpLabel label)`, `bool CreateIfAbsent(OtpLabel label, string canonicalUri)`, and `void DeleteIfPresent(OtpLabel label)`.
- Produces: `public sealed class FileOtpRepository : IOtpRepository` with `public FileOtpRepository(string rootDirectory)` and `public static FileOtpRepository FromEnvironment()`.
- Consumed by: Tasks 3 and 4.

- [ ] **Step 1: Write failing label and repository tests**

```csharp
[Fact]
public void Label_encodes_colons_for_a_portable_storage_file_name() =>
    OtpLabel.Parse("example.com:rob").ToStorageFileName().ShouldBe("example.com%3Arob");

[Theory]
[InlineData("")]
[InlineData(".")]
[InlineData("..")] 
[InlineData("CON")]
[InlineData("name.")]
public void Label_rejects_nonportable_storage_names(string value) =>
    Should.Throw<OtpValidationException>(() => OtpLabel.Parse(value));

[Fact]
public void Create_writes_utf8_uri_at_encoded_path_and_list_decodes_it()
{
    using var root = new TemporaryDirectory();
    var repository = new FileOtpRepository(root.Path);
    var label = OtpLabel.Parse("example.com:rob");

    repository.CreateIfAbsent(label, "otpauth://totp/example.com%3Arob?secret=JBSWY3DP").ShouldBeTrue();

    File.ReadAllText(Path.Combine(root.Path, "example.com%3Arob")).ShouldBe("otpauth://totp/example.com%3Arob?secret=JBSWY3DP");
    repository.List().Select(item => item.Value).ShouldBe(["example.com:rob"]);
}
```

Also test missing-root list/get/delete semantics, duplicate protection without replacement, ordinal sort, and a non-empty `OTP_CLI_OVERRIDE_DIR` taking precedence over the home-directory root.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~OtpLabelTests|FullyQualifiedName~FileOtpRepositoryTests"`

Expected: FAIL because label and repository types do not exist.

- [ ] **Step 3: Implement labels and the repository**

Use `Uri.EscapeDataString` and `Uri.UnescapeDataString`. Reject empty labels, `.`/`..`, labels ending in period or space, and case-insensitive Windows device-name stems `CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, and `LPT1`–`LPT9`. `CreateIfAbsent` is the sole method to call `Directory.CreateDirectory`; it uses `FileMode.CreateNew` and UTF-8. List ordinally sorts decoded labels. Resolve a non-empty `OTP_CLI_OVERRIDE_DIR` literally; otherwise derive `.config/otp` from `Environment.SpecialFolder.UserProfile`.

- [ ] **Step 4: Run the focused tests**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~OtpLabelTests|FullyQualifiedName~FileOtpRepositoryTests"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Cli/Otp/OtpLabel.cs src/Cli/Otp/OtpValidationException.cs src/Cli/Storage src/Tests/Cli/Otp/OtpLabelTests.cs src/Tests/Cli/Storage
git commit -m "feat: add file-backed OTP storage"
```

### Task 3: Parse, serialize, and generate TOTP credentials

**Files:**
- Create: `src/Cli/Otp/TotpCredential.cs`, `TotpGenerator.cs`
- Create: `src/Tests/Cli/Otp/TotpCredentialTests.cs`, `TotpGeneratorTests.cs`

**Interfaces:**
- Consumes: `OtpLabel`, `OtpTimestamp`, and `OtpValidationException`.
- Produces: `public sealed class TotpCredential` with `public OtpLabel Label { get; }`, `public string CanonicalUri { get; }`, `public static TotpCredential Parse(string uri)`, and `public static TotpCredential Create(OtpLabel label, string secret, int? period, string? algorithm, string? issuer)`.
- Produces: `public static class TotpGenerator` with `public static string Generate(TotpCredential credential, OtpTimestamp timestamp)`.
- Consumed by: Task 4.

- [ ] **Step 1: Write failing URI and TOTP tests**

```csharp
[Fact]
public void Parse_decodes_an_issuer_prefixed_label_for_cli_use()
{
    var credential = TotpCredential.Parse("otpauth://totp/Example%20Issuer%3A%20rob?secret=JBSWY3DP");

    credential.Label.Value.ShouldBe("Example Issuer: rob");
}

[Fact]
public void Create_keeps_the_supplied_label_when_issuer_is_present()
{
    var credential = TotpCredential.Create(OtpLabel.Parse("rob"), "JBSWY3DP", null, null, "example.com");

    credential.Label.Value.ShouldBe("rob");
    credential.CanonicalUri.ShouldContain("issuer=example.com");
}

[Theory]
[InlineData("SHA1", "94287082")]
public void Generate_matches_rfc_6238_at_59_seconds(string algorithm, string expected)
{
    var credential = TotpCredential.Parse($"otpauth://totp/rfc?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&algorithm={algorithm}&digits=8");

    TotpGenerator.Generate(credential, OtpTimestamp.FromTime(DateTime.UnixEpoch.AddSeconds(59))).ShouldBe(expected);
}
```

Add SHA-256 and SHA-512 RFC-vector tests with their RFC-specific secrets, default SHA-1/six-digit/30-second creation, Base32 case canonicalization, eight-digit parsing, period boundaries, missing secret, HOTP rejection, invalid Base32/algorithm/digits/period, duplicate known parameters with case-insensitive names, ignored extensions, and default-parameter omission from canonical output.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~TotpCredentialTests|FullyQualifiedName~TotpGeneratorTests"`

Expected: FAIL because parsing and token generation do not exist.

- [ ] **Step 3: Implement credential parsing and RFC 6238 generation**

Parse the raw label before decoding. Split at the first literal colon or case-insensitive `%3A`; decode components and rebuild issuer-prefixed labels as `issuer: account`. Require exactly one `secret`; treat known parameter names case-insensitively; ignore unknown extensions. Canonical output uses lowercase `otpauth://totp/`, fully escapes the friendly label, uppercases the Base32 secret, and orders known query parameters as `secret`, non-default `algorithm`, non-default `digits`, non-default `period`, then `issuer`.

Decode unpadded RFC 4648 Base32. Generate with `floor(unixSeconds / period)`, an 8-byte big-endian counter, selected HMAC, RFC 6238 dynamic truncation, and zero-padded modulo `10^digits` formatting.

- [ ] **Step 4: Run the focused tests**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~TotpCredentialTests|FullyQualifiedName~TotpGeneratorTests"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Cli/Otp/TotpCredential.cs src/Cli/Otp/TotpGenerator.cs src/Tests/Cli/Otp/TotpCredentialTests.cs src/Tests/Cli/Otp/TotpGeneratorTests.cs
git commit -m "feat: add TOTP credential generation"
```

### Task 4: Add application use cases and clipboard adapter

**Files:**
- Create: `src/Cli/Clipboard/IOtpClipboard.cs`, `TextCopyOtpClipboard.cs`
- Create: `src/Cli/Application/OtpCommandOutcome.cs`, `OtpCommandService.cs`
- Create: `src/Tests/Cli/Application/OtpCommandServiceTests.cs`
- Modify: `src/Cli/RobertHodgen.Otp.Cli.csproj`, `src/Cli/CliExitCodes.cs`

**Interfaces:**
- Consumes: `ISystemClock`, `IOtpRepository`, `TotpCredential`, `TotpGenerator`, `OtpLabel`, and `OtpValidationException`.
- Produces: `public interface IOtpClipboard { void Copy(string text); }`; `public sealed class TextCopyOtpClipboard : IOtpClipboard`; `public sealed record OtpCommandOutcome(int ExitCode, string? StandardOutput, string? StandardError)`.
- Produces: `public sealed class OtpCommandService` with `List()`, `Create(string labelOrUrl, string? secret, int? period, string? algorithm, string? issuer)`, `Read(string label)`, `Copy(string label)`, and `Delete(IReadOnlyList<string> labels)`, each returning `OtpCommandOutcome`.
- Consumed by: Task 5.

- [ ] **Step 1: Write failing application-service tests**

```csharp
[Fact]
public void List_returns_total_zero_when_the_repository_is_empty() =>
    service.List().ShouldBe(new OtpCommandOutcome(0, $"total 0{Environment.NewLine}", null));

[Fact]
public void Read_returns_the_fixed_clock_token_without_exposing_the_uri()
{
    var outcome = service.Read("example.com");

    outcome.ExitCode.ShouldBe(0);
    outcome.StandardOutput.ShouldBe("94287082" + Environment.NewLine);
    outcome.StandardError.ShouldBeNull();
}

[Fact]
public void Read_missing_label_returns_the_required_safe_error() =>
    service.Read("missing").ShouldBe(new OtpCommandOutcome(1, null, "no OTP with that label was found" + Environment.NewLine));

[Fact]
public void Copy_passes_the_generated_token_to_the_clipboard()
{
    service.Copy("example.com");

    clipboard.CopiedText.ShouldBe("94287082");
}
```

Add tests for URI and explicit-secret creation, silent successful create/copy/delete, duplicate-create preservation, corrupt stored credentials that do not expose URI/secret, missing labels, clipboard exceptions, idempotent deletion, and an I/O failure part-way through multi-label deletion. Test clipboard behavior exclusively through a fake `IOtpClipboard`; do not invoke or read the real clipboard in tests.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~OtpCommandServiceTests"`

Expected: FAIL because use cases and the clipboard boundary do not exist.

- [ ] **Step 3: Implement outcomes, use cases, and clipboard support**

Add `<PackageReference Include="TextCopy" Version="6.2.1" />`. Define `CliExitCodes.Success = 0` and `CliExitCodes.Failure = 1`. `OtpCommandService` maps domain, repository, and clipboard failures to non-secret outcomes, generates with `_clock.Now`, emits exactly `no OTP with that label was found` for missing reads, makes absent deletes successful, and returns failure for I/O exceptions. `TextCopyOtpClipboard.Copy` delegates to TextCopy's text-set API; the service catches adapter exceptions and emits no token.

- [ ] **Step 4: Run the focused tests**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~OtpCommandServiceTests"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Cli/Application src/Cli/Clipboard src/Cli/CliExitCodes.cs src/Cli/RobertHodgen.Otp.Cli.csproj src/Tests/Cli/Application
git commit -m "feat: add OTP command use cases"
```

### Task 5: Wire the use cases into System.CommandLine

**Files:**
- Create: `src/Cli/Application/CliDependencies.cs`
- Modify: `src/Cli/CliApplication.cs`, `src/Tests/Cli/CliApplicationTests.cs`

**Interfaces:**
- Consumes: `OtpCommandService`, `OtpCommandOutcome`, `FileOtpRepository.FromEnvironment()`, `RunningSystemClock`, `TextCopyOtpClipboard`, and the existing command symbols.
- Produces: `public sealed class CliDependencies` containing `OtpCommandService Service`, `TextWriter Output`, and `TextWriter Error`; `public static RootCommand CreateRootCommand(CliDependencies dependencies)` in addition to the production convenience overload.
- Consumed by: `Program.cs` unchanged and Task 6 process tests.

- [ ] **Step 1: Write failing command-wiring tests**

```csharp
[Fact]
public void Read_writes_only_the_service_output_to_the_configured_writer()
{
    using var output = new StringWriter();
    using var error = new StringWriter();
    var command = CliApplication.CreateRootCommand(dependenciesWithKnownCredential(output, error));

    command.Parse(["read", "example.com"]).Invoke().ShouldBe(0);

    output.ToString().ShouldBe("94287082" + Environment.NewLine);
    error.ToString().ShouldBeEmpty();
}

[Fact]
public void Create_preserves_the_existing_command_symbol_contract()
{
    var create = GetSubcommand("create");

    create.Options.Select(option => option.Name).ShouldBe(new[] { "--secret", "--period", "--algorithm", "--issuer" });
}
```

Replace placeholder-handler tests with execution tests for all commands. Retain parser arity/type tests.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~CliApplicationTests"`

Expected: FAIL because handlers emit the not-implemented diagnostic and there is no dependency overload.

- [ ] **Step 3: Replace placeholders with injected service handlers**

Keep current command names, arguments, options, and arity. The current `CreateRootCommand(TextWriter? error = null)` builds production dependencies and uses `Console.Out`; the new overload receives explicit dependencies. Each handler retrieves values, invokes the service, writes non-null outcome output/error to configured writers, and returns the outcome exit code. Remove `ConfigurePlaceholder` and `NotImplemented` use.

- [ ] **Step 4: Run the focused tests**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~CliApplicationTests"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Cli/Application/CliDependencies.cs src/Cli/CliApplication.cs src/Tests/Cli/CliApplicationTests.cs
git commit -m "feat: implement OTP CLI commands"
```

### Task 6: Add isolated process-level CRUD tests

**Files:**
- Create: `src/Tests/Cli/EndToEnd/TemporaryDirectory.cs`, `CliProcess.cs`, `OtpCliEndToEndTests.cs`

**Interfaces:**
- Consumes: built `otp.dll`, `OTP_CLI_OVERRIDE_DIR`, and the Task 5 command contract.
- Produces: disposable `TemporaryDirectory` with `string Path`; `CliProcess.WithOverrideDirectory(string path).Invoke(params string[] arguments)` returning captured exit code, stdout, and stderr.
- Consumed by: Task 7.

- [ ] **Step 1: Write failing process-level tests**

```csharp
[Fact]
public void List_with_a_missing_override_directory_reports_an_empty_store()
{
    using var root = new TemporaryDirectory(createImmediately: false);

    var result = CliProcess.WithOverrideDirectory(root.Path).Invoke("list");

    result.ExitCode.ShouldBe(0);
    result.StandardOutput.ShouldBe("total 0" + Environment.NewLine);
    result.StandardError.ShouldBeEmpty();
}

[Fact]
public void Create_list_read_and_delete_use_the_override_directory()
{
    using var root = new TemporaryDirectory();
    var cli = CliProcess.WithOverrideDirectory(root.Path);

    cli.Invoke("create", "otpauth://totp/example.com?secret=JBSWY3DPFQQHO33SNRSCC").ExitCode.ShouldBe(0);
    cli.Invoke("list").StandardOutput.ShouldBe("total 1" + Environment.NewLine + "example.com" + Environment.NewLine);
    cli.Invoke("read", "example.com").StandardOutput.ShouldMatch("^[0-9]{6}\\r?\\n$");
    cli.Invoke("delete", "example.com").ExitCode.ShouldBe(0);
    cli.Invoke("list").StandardOutput.ShouldBe("total 0" + Environment.NewLine);
}
```

Add seed-file coverage at `<override>/example.com%3Arob` proving `list`, `read`, and `delete` accept/display `example.com:rob`, plus missing-label read exit code one and the exact stderr. Do not add an E2E `copy` test.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~OtpCliEndToEndTests"`

Expected: FAIL because the child-process helper and tests do not exist.

- [ ] **Step 3: Implement E2E helpers**

`TemporaryDirectory` owns and deletes only its unique temp-root path. `CliProcess` starts `dotnet` with `typeof(CliApplication).Assembly.Location`, uses `ProcessStartInfo.ArgumentList`, sets the override only in child environment, redirects streams, waits for exit, and returns capture. No test may touch `~/.config/otp` or the system clipboard.

- [ ] **Step 4: Run the focused tests**

Run: `dotnet test otp-cli.slnx --filter "FullyQualifiedName~OtpCliEndToEndTests"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Tests/Cli/EndToEnd
git commit -m "test: add OTP CLI end-to-end coverage"
```

### Task 7: Mark complete TODO work and verify the complete suite

**Files:**
- Modify: `TODO.md`

**Interfaces:**
- Consumes: completed Tasks 1–6.
- Produces: accurate completion state for the command implementation work.

- [ ] **Step 1: Review TODO completion against tests**

Confirm passing coverage exists for the override directory, empty list, create, one-entry list, read, and delete E2E rows, plus every implementation row. Confirm the copy E2E row remains unchecked.

- [ ] **Step 2: Run the full test suite before documentation update**

Run: `make test`

Expected: PASS. If the repository still lacks the documented Makefile, run `dotnet test otp-cli.slnx` and record the discrepancy; do not recreate unrelated build automation.

- [ ] **Step 3: Update `TODO.md`**

Check override-directory, empty-list/create/list/read/delete E2E rows and all five implementation rows. Leave the `otp copy` E2E row unchecked. Do not change unrelated items.

- [ ] **Step 4: Run CI-equivalent Release verification**

Run: `dotnet restore otp-cli.slnx && dotnet build otp-cli.slnx --configuration Release --no-restore && dotnet test otp-cli.slnx --configuration Release --no-build --no-restore`

Expected: all commands succeed; Release tests pass.

- [ ] **Step 5: Commit**

```bash
git add TODO.md
git commit -m "docs: mark implemented OTP commands complete"
```

## Plan Self-Review

- **Spec coverage:** Tasks 1–3 cover time, labels, URI parsing, canonical persistence, and TOTP calculation. Task 2 covers root selection and filesystem semantics. Task 4 covers use cases and TextCopy. Task 5 activates the CLI. Task 6 covers agreed process-level CRUD. Task 7 updates TODO state and runs Release verification.
- **Step scan:** Every task starts from named assertions, runs a failing focused command, declares interfaces, implements one cohesive deliverable, reruns the test, and ends in a scoped commit.
- **Type consistency:** Task 1 time flows to Tasks 3–4; Task 2 labels/repository flow to Tasks 3–4; Task 4 outcomes/service flow to Task 5 handlers.
- **Review focus:** Each listed concern is assigned to a Task 2, 3, or 4 test.
- **Proportion:** The plan locks architecture, signatures, tests, output, and persistence decisions without transcribing production method bodies.
