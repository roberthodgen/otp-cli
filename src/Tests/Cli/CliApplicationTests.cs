namespace RobertHodgen.Otp.Cli.Tests;

using System.CommandLine;
using Cli.Application;
using Cli.Otp;
using Cli.Storage;
using Cli.Time;
using Clipboard;

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

    [Fact]
    public void Create_without_secret_returns_a_validation_error()
    {
        using var error = new StringWriter();

        var exitCode = CliApplication.CreateRootCommand(error)
            .Parse(["create", "example.com"])
            .Invoke();

        exitCode.ShouldBe(CliExitCodes.Failure);
        error.ToString().ShouldBe("a secret is required when creating an OTP from a label" + Environment.NewLine);
    }

    [Fact]
    public void Read_writes_only_the_service_output_to_the_configured_writer()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var repository = new InMemoryRepository
        {
            ["example.com"] = "otpauth://totp/example.com?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&digits=8",
        };
        var dependencies = new CliDependencies(
            new OtpCommandService(repository, KnownSystemClock.FromTime(DateTime.UnixEpoch.AddSeconds(59)), new NullClipboard()),
            output,
            error);

        var exitCode = CliApplication.CreateRootCommand(dependencies).Parse(["read", "example.com"]).Invoke();

        exitCode.ShouldBe(0);
        output.ToString().ShouldBe("94287082" + Environment.NewLine);
        error.ToString().ShouldBeEmpty();
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

    private static Command GetSubcommand(string name) =>
        CliApplication.CreateRootCommand().Subcommands.Single(command => command.Name == name);

    private sealed class NullClipboard : IOtpClipboard
    {
        public void Copy(string text)
        {
        }
    }

    private sealed class InMemoryRepository : IOtpRepository
    {
        private readonly Dictionary<string, string> _entries = new(StringComparer.Ordinal);

        public string this[string label]
        {
            get => _entries[label];
            set => _entries[label] = value;
        }

        public IReadOnlyList<OtpLabel> List() => _entries.Keys.Select(OtpLabel.Parse).ToArray();

        public string? Get(OtpLabel label) => _entries.GetValueOrDefault(label.Value);

        public bool CreateIfAbsent(OtpLabel label, string canonicalUri) => _entries.TryAdd(label.Value, canonicalUri);

        public void DeleteIfPresent(OtpLabel label) => _entries.Remove(label.Value);
    }
}
