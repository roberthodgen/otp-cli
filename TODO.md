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
  - [ ] Ability to provide `OTP_CLI_OVERRIDE_DIR=temp-dir` for testing full CRUD
  - Test `otp list` w/o any, e.g. a known empty directory
  - Test `otp create` w/ a known value, e.g.: `otp create otpauth://totp/example.com?secret=JBSWY3DPFQQHO33SNRSCC`
  - Test `otp list` w/ one entry
  - Test `otp read` w/ a known value, e.g.: `otp read example.com`
  - Test `otp copy` w/ a known value, e.g.: `otp copy example.com`
  - Test `otp delete` w/ a known value, e.g.: `otp delete example.com`

- [ ] Implement `otp list`
- [ ] Implement `otp create`
- [ ] Implement `otp read`
- [ ] Implement `otp copy`
- [ ] Implement `otp delete`
