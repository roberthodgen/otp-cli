# One-time Password (OTP) Command Line Interface (CLI)

## Get and Install

Visit the [releases](https://github.com/roberthodgen/otp-cli/releases) page to download the latest release.

Move to your "bin" directory and extract if needed.

## Usage

A simple `otp` command can be invoked with 5 commands: `list`, `create`, `read`, `copy`, and `delete`.

### List

View all saved one-time passwords.

```bash
otp list
```

Example output:

```
$ otp list
total 1
example.com
```

### Create

Create a new one-time password.

```bash
otp create example.com --secret=JBSWY3DPFQQHO33SNRSCC
```

Create syntax:

```
otp create <label-or-url> [--secret] [--period] [--algorithm] [--issuer]
```

Note: `--secret` is required if `label-or-url` argument is not a valid `otpauth://` URL.

For example, an OTP can be created using the `otpauth://` URL scheme:

```bash
otp create otpauth://totp/example.com?secret=JBSWY3DPFQQHO33SNRSCC
```

### Read

Read a single one-time password.

```bash
otp read example.com
```

Example output:

```
$ otp read example.com
123456
```

The simple output of just the digits allows you to pipe this into `pbcopy` for example.

### Copy

Copies a single one-time password to your pasteboard.

```bash
otp copy example.com
```

Example output: none

### Delete

Removes one or more one-time passwords.

```bash
otp delete example.com
```

Multiple arguments can be supplied to remove multiple values, e.g.:

```bash
otp delete example1.com example2.com example3.com
```

Example output: none

## Data Storage

All 1-time passwords are stored in your `~/.config/otp` directory with the label being URL-encoded according to RFC3986.

## Notes

- `otpauth://` scheme reference: https://github.com/andesco/otpauth-uri/blob/main/draft-andesco-otpauth-uri.md
