# Repository Guidance

- This repository is currently a scaffold: `otp-cli.slnx` has no projects and `src/` is empty. Do not assume build, test, lint, or formatting commands exist yet.
- `README.md` describes the intended CLI behavior, not implemented functionality. Treat `TODO.md` as the source for planned tooling and test requirements.
- When implementing end-to-end tests, support `OTP_CLI_OVERRIDE_DIR` so tests can isolate OTP storage from `~/.config/otp`.
