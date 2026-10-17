# Repository Guidance

- `otp-cli.slnx` contains the .NET 10 CLI project at `src/Cli/Cli.csproj` and its test project at `src/Tests/Cli/Cli.Tests.csproj`.
- The executable assembly is named `otp`; production code uses the `RobertHodgen.Otp.Cli` namespace and tests use `RobertHodgen.Otp.Cli.Tests`.
- Run `make build` to build the solution and `make test` to execute the xUnit v3 + Shouldly test suite.
- `CliApplication.CreateRootCommand()` defines the current `System.CommandLine` API: `list`, `create`, `read`, `copy`, and `delete`. Handlers intentionally emit a not-implemented diagnostic to standard error and return exit code `1` until OTP functionality is implemented.
- The command model is scaffold-only: do not add OTP parsing, storage, generation, clipboard integration, or semantic validation without the corresponding implementation work and tests.
- `README.md` describes the intended CLI behavior, not implemented functionality. Treat `TODO.md` as the source for planned tooling and test requirements.
- When implementing end-to-end tests, support `OTP_CLI_OVERRIDE_DIR` so tests can isolate OTP storage from `~/.config/otp`.
