# TODO

- [x] GitHub CI/CD
  - [x] PR template
  - [x] GitHub action: CI run unit tests on PR
  - [x] GitHub action: CD creates build artifacts + GitHub release

- [ ] .NET CLI
  - [x] .NET 10
  - [x] System.CommandLine
  - [x] xUnit + Shouldly unit tests

- [ ] End-to-end tests
  - [x] Ability to provide `OTP_CLI_OVERRIDE_DIR=temp-dir` for testing full CRUD
  - [x] Test `otp list` w/o any, e.g. a known empty directory
  - [x] Test `otp create` w/ a known value, e.g.: `otp create otpauth://totp/example.com?secret=JBSWY3DPFQQHO33SNRSCC`
  - [x] Test `otp list` w/ one entry
  - [x] Test `otp read` w/ a known value, e.g.: `otp read example.com`
  - [ ] Test `otp copy` w/ a known value, e.g.: `otp copy example.com`
  - [x] Test `otp delete` w/ a known value, e.g.: `otp delete example.com`

- [x] Implement `otp list`
- [x] Implement `otp create`
- [x] Implement `otp read`
- [x] Implement `otp copy`
- [x] Implement `otp delete`
- [ ] Implement `otp create --secret-raw` for non-base32 encoded secrets
