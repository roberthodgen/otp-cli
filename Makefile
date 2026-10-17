.PHONY: build test

build:
	dotnet build otp-cli.slnx

test:
	dotnet test otp-cli.slnx
