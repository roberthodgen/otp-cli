using Shouldly;
using System.CommandLine;
using Xunit;

namespace RobertHodgen.Otp.Cli.Tests;

public sealed class CliApplicationTests
{
    [Fact]
    public void CreateRootCommand_returns_root_command()
    {
        var command = CliApplication.CreateRootCommand();

        command.ShouldBeOfType<RootCommand>();
    }

    [Fact]
    public void CreateRootCommand_registers_expected_commands_in_order()
    {
        var command = CliApplication.CreateRootCommand();

        command.Subcommands.Select(child => child.Name)
            .ShouldBe(new[] { "list", "create", "read", "copy", "delete" });
    }

    [Fact]
    public void CreateRootCommand_configures_create_input_symbols()
    {
        var create = GetSubcommand("create");

        var labelOrUrl = create.Arguments.ShouldHaveSingleItem();
        labelOrUrl.Name.ShouldBe("label-or-url");
        labelOrUrl.ShouldBeOfType<Argument<string>>();
        labelOrUrl.Arity.ShouldBe(ArgumentArity.ExactlyOne);

        create.Options.Select(option => option.Name)
            .ShouldBe(new[] { "--secret", "--period", "--algorithm", "--issuer" });
        create.Options.ShouldAllBe(option => option.Arity.Equals(ArgumentArity.ExactlyOne));
        create.Options.Single(option => option.Name == "--secret")
            .ShouldBeOfType<Option<string>>();
        create.Options.Single(option => option.Name == "--period")
            .ShouldBeOfType<Option<int>>();
        create.Options.Single(option => option.Name == "--algorithm")
            .ShouldBeOfType<Option<string>>();
        create.Options.Single(option => option.Name == "--issuer")
            .ShouldBeOfType<Option<string>>();
    }

    [Theory]
    [InlineData("read")]
    [InlineData("copy")]
    public void CreateRootCommand_configures_exactly_one_label_for_single_label_commands(string commandName)
    {
        var command = GetSubcommand(commandName);

        var label = command.Arguments.ShouldHaveSingleItem();
        label.Name.ShouldBe("label");
        label.ShouldBeOfType<Argument<string>>();
        label.Arity.ShouldBe(ArgumentArity.ExactlyOne);
    }

    [Fact]
    public void CreateRootCommand_configures_one_or_more_labels_for_delete()
    {
        var delete = GetSubcommand("delete");

        var labels = delete.Arguments.ShouldHaveSingleItem();
        labels.Name.ShouldBe("labels");
        labels.ShouldBeOfType<Argument<string[]>>();
        labels.Arity.ShouldBe(ArgumentArity.OneOrMore);
    }

    [Theory]
    [MemberData(nameof(InvalidCommandLines))]
    public void Invalid_required_arguments_and_period_report_parser_errors(string[] arguments)
    {
        var result = CliApplication.CreateRootCommand().Parse(arguments);

        result.Errors.ShouldNotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(PlaceholderCommandLines))]
    public void Placeholder_command_writes_diagnostic_and_returns_not_implemented(
        string commandName,
        string[] arguments)
    {
        using var error = new StringWriter();

        var exitCode = CliApplication.CreateRootCommand(error).Parse(arguments).Invoke();

        exitCode.ShouldBe(CliExitCodes.NotImplemented);
        error.ToString().ShouldBe($"otp {commandName} is not implemented.{Environment.NewLine}");
    }

    [Fact]
    public void Create_without_secret_reaches_placeholder_handler()
    {
        using var error = new StringWriter();

        var exitCode = CliApplication.CreateRootCommand(error)
            .Parse(["create", "example.com"])
            .Invoke();

        exitCode.ShouldBe(CliExitCodes.NotImplemented);
        error.ToString().ShouldBe($"otp create is not implemented.{Environment.NewLine}");
    }

    public static IEnumerable<object[]> InvalidCommandLines =>
    [
        [new[] { "delete" }],
        [new[] { "read" }],
        [new[] { "read", "first", "second" }],
        [new[] { "create", "value", "--period", "invalid" }],
        [new[] { "create", "value", "--secret" }],
        [new[] { "create", "value", "--period" }],
        [new[] { "create", "value", "--algorithm" }],
        [new[] { "create", "value", "--issuer" }],
    ];

    public static IEnumerable<object[]> PlaceholderCommandLines =>
    [
        ["list", new[] { "list" }],
        ["read", new[] { "read", "example.com" }],
        ["copy", new[] { "copy", "example.com" }],
        ["delete", new[] { "delete", "example.com" }],
    ];

    private static Command GetSubcommand(string name) =>
        CliApplication.CreateRootCommand().Subcommands.Single(command => command.Name == name);
}
