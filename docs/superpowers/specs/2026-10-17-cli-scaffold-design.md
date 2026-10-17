# .NET CLI Scaffold Design

## Intent

Create the first executable project for the OTP command-line application. The
scaffold establishes the public command surface and a testable command model;
it intentionally does not implement OTP parsing, storage, code generation, or
clipboard integration.

## Scope

- Target .NET 10.
- Create an executable project in `src/Cli` with root namespace
  `RobertHodgen.Otp.Cli`.
- Use `System.CommandLine` to declare the documented command API.
- Create xUnit and Shouldly unit tests in `src/Tests/Cli` with namespace
  `RobertHodgen.Otp.Cli.Tests`.
- Add `build` and `test` targets to a root `Makefile`.

The end-to-end testing override (`OTP_CLI_OVERRIDE_DIR`), storage behavior,
and the CI/CD tasks remain outside this increment.

## Command Model

`CliApplication.CreateRootCommand()` creates the root command for the `otp`
executable with exactly these child commands. System.CommandLine derives a
root command's display name from its host process, so the executable project
sets its assembly name to `otp` rather than attempting to set `RootCommand.Name`.

| Command | Arguments | Options |
| --- | --- | --- |
| `list` | none | none |
| `create` | required `label-or-url` | optional `--secret`, `--period`, `--algorithm`, `--issuer` |
| `read` | required `label` | none |
| `copy` | required `label` | none |
| `delete` | required one-or-more `labels` | none |

`label-or-url`, `label`, and `labels` are string arguments. `--secret`,
`--algorithm`, and `--issuer` are string options; `--period` is an integer
option. The scaffold delegates syntactic required-argument handling to
`System.CommandLine`. Semantic validation, including requiring `--secret`
when `label-or-url` is not an `otpauth://` URI, is deferred until OTP behavior
is implemented.

## Execution and Errors

`Program.cs` invokes the root command returned by `CliApplication`. Every
scaffold command has a placeholder handler that emits a concise diagnostic to
standard error and returns the named non-success `NotImplemented` exit code.
`System.CommandLine` continues to produce its own parse errors for malformed
command input.

## Project Structure

```text
otp-cli.slnx
Makefile
src/
  Cli/
    Cli.csproj
    Program.cs
    CliApplication.cs
  Tests/
    Cli/
      Cli.Tests.csproj
      CliApplicationTests.cs
```

The solution includes both projects. The test project references the CLI
project and uses xUnit v3, Shouldly, and the .NET test SDK. The executable
assembly is named `otp`.

## Tests

Command-definition tests exercise `CliApplication.CreateRootCommand()` without
launching a process or accessing the file system. They verify:

- root and child command names, including the absence of unintended children;
- positional argument names and arity;
- `create` option names and value types;
- the placeholder-handler diagnostic and non-success exit behavior.

These tests specify the command API, not OTP functionality. Functional,
process-level, and CRUD end-to-end tests—including use of
`OTP_CLI_OVERRIDE_DIR`—will be added with their corresponding implementation.

## Build Interface

The root `Makefile` offers:

- `make build`: builds the solution;
- `make test`: runs all solution tests.

Both targets use the .NET CLI and require an installed .NET 10 SDK.
