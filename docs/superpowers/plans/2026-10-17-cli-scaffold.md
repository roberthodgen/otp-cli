# .NET CLI Scaffold Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a .NET 10 `otp` executable whose `System.CommandLine` model exposes the documented command API and is covered by command-definition unit tests.

**Architecture:** `CliApplication` is the sole command-composition boundary. It returns the `RootCommand` for an executable assembly named `otp`, while `Program.cs` only parses and invokes that model. Tests construct the command model in-process and assert its metadata and placeholder invocation behavior without starting a child process or accessing OTP storage.

**Tech Stack:** .NET 10, C# / `net10.0`, System.CommandLine 2.0.13, xUnit v3 3.2.2 (`xunit.v3`), Shouldly 4.3.0, Microsoft.NET.Test.Sdk 18.11.0, xunit.runner.visualstudio 3.1.5, Make.

**Spec:** `docs/superpowers/specs/2026-10-17-cli-scaffold-design.md`

## Global Constraints

- Target .NET 10 (`net10.0`) and place the executable project in `src/Cli`.
- Set the production root namespace to `RobertHodgen.Otp.Cli`.
- Place the xUnit v3/Shouldly test project in `src/Tests/Cli` with namespace `RobertHodgen.Otp.Cli.Tests`.
- Use the stable `System.CommandLine` 2.0.13 package.
- Implement exactly `list`, `create`, `read`, `copy`, and `delete`; do not implement OTP parsing, storage, generation, clipboard integration, CI/CD, or `OTP_CLI_OVERRIDE_DIR` in this increment.
- Keep semantic validation deferred: `create` must not yet require `--secret` for a non-`otpauth://` value.
- Provide root Make targets named `build` and `test` that use the solution.

## Review Focus

- `otp delete` with no labels must fail parsing because `labels` is one-or-more (Task 2).
- `otp read` with zero or more than one label must fail parsing because `label` is exactly one (Task 2).
- `otp create value --period invalid` must fail parsing because `--period` accepts an integer (Task 2).
- `otp create value` must parse without a `--secret` option; semantic secret validation is intentionally deferred (Task 3).
- Each declared command must return the same non-success placeholder exit code rather than accidentally succeeding (Task 3).

---

### Task 1: Bootstrap the executable, test project, and root command

**Files:**
- Modify: `otp-cli.slnx`
- Create: `src/Cli/Cli.csproj`
- Create: `src/Cli/CliApplication.cs`
- Create: `src/Cli/Program.cs`
- Create: `src/Tests/Cli/Cli.Tests.csproj`
- Create: `src/Tests/Cli/CliApplicationTests.cs`
- Create: `Makefile`

**Interfaces:**
- Produces: `public static class CliApplication` in namespace `RobertHodgen.Otp.Cli`.
- Produces: `public static RootCommand CreateRootCommand(TextWriter? error = null)`; when called without an argument it uses `Console.Error`.
- Produces: a `RootCommand`; the executable project assembly name is `otp`, because System.CommandLine derives the root display name from the host process. Task 2 adds the subcommands.
- Consumes: System.CommandLine 2.0.13.

- [ ] **Step 1: Add executable and test project configuration**

Create both SDK-style projects targeting `net10.0`, enabling nullable reference types and implicit usings. Make `Cli.csproj` an executable with package reference `System.CommandLine` 2.0.13. Make `Cli.Tests.csproj` non-packable, reference `../../Cli/Cli.csproj`, and add package references for Microsoft.NET.Test.Sdk 18.11.0, `xunit.v3` 3.2.2, xunit.runner.visualstudio 3.1.5 (private assets), and Shouldly 4.3.0. Add both projects to `otp-cli.slnx`.

- [ ] **Step 2: Write the failing root-command definition test**

In `CliApplicationTests.cs`, write `CreateRootCommand_returns_root_command`:

```csharp
var command = CliApplication.CreateRootCommand();

command.ShouldBeOfType<RootCommand>();
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test src/Tests/Cli/Cli.Tests.csproj --filter "FullyQualifiedName~CreateRootCommand_returns_root_command"`

Expected: FAIL because `CliApplication` does not exist.

- [ ] **Step 4: Implement the minimal root command factory**

In `src/Cli/CliApplication.cs`, implement `CliApplication.CreateRootCommand(TextWriter? error = null)` using `RootCommand`; retain the selected error writer for Task 3's handler closures. Set `Cli.csproj`'s assembly name to `otp` so the root command receives the intended executable name at runtime. In `src/Cli/Program.cs`, parse `args` against `CliApplication.CreateRootCommand()` and invoke the parse result to return its exit code.

- [ ] **Step 5: Add Make build and test targets**

Create a root `Makefile` where `build` runs `dotnet build otp-cli.slnx` and `test` runs `dotnet test otp-cli.slnx`.

- [ ] **Step 6: Run the focused test and build verification**

Run: `dotnet test src/Tests/Cli/Cli.Tests.csproj --filter "FullyQualifiedName~CreateRootCommand_returns_root_command" && make build && make test`

Expected: all commands succeed.

- [ ] **Step 7: Commit the bootstrap**

```bash
git add otp-cli.slnx Makefile src/Cli src/Tests/Cli
git commit -m "feat: scaffold OTP CLI projects"
```

### Task 2: Define the five command shapes and parser contracts

**Files:**
- Modify: `src/Cli/CliApplication.cs`
- Modify: `src/Tests/Cli/CliApplicationTests.cs`

**Interfaces:**
- Consumes: `CliApplication.CreateRootCommand(TextWriter? error = null)` from Task 1.
- Produces: `list`, `create`, `read`, `copy`, and `delete` as the root command's only subcommands.
- Produces: `create` required string `label-or-url`; optional string `--secret`, optional integer `--period`, optional string `--algorithm`, optional string `--issuer`; `read` and `copy` required string `label`; `delete` required one-or-more `string[]` `labels`.

- [ ] **Step 1: Write failing command-definition and parsing tests**

Add tests which construct the root command and assert:

```csharp
command.Subcommands.Select(child => child.Name)
    .ShouldBe(new[] { "list", "create", "read", "copy", "delete" });

create.Arguments.Single().Name.ShouldBe("label-or-url");
create.Options.Select(option => option.Name)
    .ShouldBe(new[] { "--secret", "--period", "--algorithm", "--issuer" });
create.Options.Single(option => option.Name == "--period")
    .ShouldBeOfType<Option<int>>();
```

Add dedicated tests asserting `ArgumentArity.ExactlyOne` for `label-or-url` and each `label`, `ArgumentArity.OneOrMore` for `labels`, and `ArgumentArity.ZeroOrOne` for each create option. Also assert parser errors for `delete` with no labels, `read` with no label or two labels, and `create value --period invalid`.

- [ ] **Step 2: Run the command-shape tests to verify they fail**

Run: `dotnet test src/Tests/Cli/Cli.Tests.csproj --filter "FullyQualifiedName~CliApplicationTests"`

Expected: FAIL because no child commands or input symbols are registered.

- [ ] **Step 3: Implement the command definitions in `CliApplication.CreateRootCommand`**

Create five `Command` instances and attach them to the root in this order: `list`, `create`, `read`, `copy`, `delete`. Register symbols with the exact names and arities from the Interfaces block. Use `Option<string>` for `--secret`, `--algorithm`, and `--issuer`; use `Option<int>` for `--period`. Do not add aliases or validation beyond System.CommandLine's type and arity parsing.

- [ ] **Step 4: Run the command-definition and parser tests**

Run: `dotnet test src/Tests/Cli/Cli.Tests.csproj --filter "FullyQualifiedName~CliApplicationTests"`

Expected: PASS, including parser failures for invalid arity and non-integer period input.

- [ ] **Step 5: Commit the command API**

```bash
git add src/Cli/CliApplication.cs src/Tests/Cli/CliApplicationTests.cs
git commit -m "feat: define OTP CLI commands"
```

### Task 3: Add deferred command handlers and invocation tests

**Files:**
- Create: `src/Cli/CliExitCodes.cs`
- Modify: `src/Cli/CliApplication.cs`
- Modify: `src/Tests/Cli/CliApplicationTests.cs`

**Interfaces:**
- Consumes: all five commands and the optional `TextWriter` parameter from `CliApplication.CreateRootCommand`.
- Produces: `public static class CliExitCodes` with `public const int NotImplemented = 1`.
- Produces: each command invocation writes `otp <command> is not implemented.` followed by a newline to the supplied error writer and returns `CliExitCodes.NotImplemented`.

- [ ] **Step 1: Write failing placeholder-handler tests**

Add a theory that creates a `StringWriter`, invokes `CliApplication.CreateRootCommand(error)` with each valid argument set (`list`; `create example.com`; `read example.com`; `copy example.com`; `delete example.com`), and asserts:

```csharp
exitCode.ShouldBe(CliExitCodes.NotImplemented);
error.ToString().ShouldBe($"otp {commandName} is not implemented.{Environment.NewLine}");
```

Add `Create_without_secret_reaches_placeholder_handler` using `create example.com` to pin the intentionally deferred semantic validation.

- [ ] **Step 2: Run the handler tests to verify they fail**

Run: `dotnet test src/Tests/Cli/Cli.Tests.csproj --filter "FullyQualifiedName~placeholder|FullyQualifiedName~without_secret"`

Expected: FAIL because command actions and `CliExitCodes` do not exist.

- [ ] **Step 3: Implement the shared placeholder action**

Create `CliExitCodes.NotImplemented` with value `1`. In `CliApplication`, assign every command an action that writes the exact diagnostic from the Interfaces block to the selected `TextWriter` and returns that exit code. Do not inspect parsed values or implement semantic validation.

- [ ] **Step 4: Run all tests and Make targets**

Run: `dotnet test otp-cli.slnx && make build && make test`

Expected: all unit tests and both Make targets succeed.

- [ ] **Step 5: Commit the placeholder behavior**

```bash
git add src/Cli/CliApplication.cs src/Cli/CliExitCodes.cs src/Tests/Cli/CliApplicationTests.cs
git commit -m "feat: add CLI placeholder handlers"
```
