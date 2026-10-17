# Repository Guidance

## Build and test

- `otp-cli.slnx` contains the .NET 10 CLI project at `src/Cli/RobertHodgen.Otp.Cli.csproj` and its xUnit v3 + Shouldly test project at `src/Tests/Cli/RobertHodgen.Otp.Cli.Tests.csproj`.
- Run `make build` and `make test` for the full local build and test suite. Run `dotnet test otp-cli.slnx --filter "FullyQualifiedName~CliApplicationTests"` to focus on the current CLI tests.
- Pull-request CI restores first, then builds and tests Release configuration with `--no-restore` / `--no-build`; preserve that sequence when changing build automation.

## CLI scaffold

- The executable assembly is `otp`; production code uses `RobertHodgen.Otp.Cli` and tests use `RobertHodgen.Otp.Cli.Tests`.
- `src/Cli/Program.cs` invokes `CliApplication.CreateRootCommand()`, which owns the current `System.CommandLine` contract: `list`, `create`, `read`, `copy`, and `delete`.
- This is scaffold-only: every command handler writes its not-implemented diagnostic to standard error and returns exit code `1`. Do not add OTP parsing, storage, generation, clipboard integration, or semantic validation without the corresponding implementation and tests.
- `README.md` is aspirational; use `TODO.md` as the source of planned tooling and test requirements. End-to-end tests must support `OTP_CLI_OVERRIDE_DIR` to isolate storage from `~/.config/otp`.

## Releases

- A `v*` tag triggers release CI. Its tag must exactly match the root `package.json` version with a `v` prefix (for example, `1.2.3` → `v1.2.3`); see `docs/releasing.md` before changing release artifacts or macOS signing/notarization.
