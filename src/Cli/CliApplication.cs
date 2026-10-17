using System.CommandLine;

namespace RobertHodgen.Otp.Cli;

public static class CliApplication
{
    public static RootCommand CreateRootCommand(TextWriter? error = null)
    {
        var errorWriter = error ?? Console.Error;

        var rootCommand = new RootCommand();

        rootCommand.Add(CreatePlaceholderCommand("list", errorWriter));
        rootCommand.Add(CreateCreateCommand(errorWriter));
        rootCommand.Add(CreateSingleLabelCommand("read", errorWriter));
        rootCommand.Add(CreateSingleLabelCommand("copy", errorWriter));
        rootCommand.Add(CreateDeleteCommand(errorWriter));

        return rootCommand;
    }

    private static Command CreateCreateCommand(TextWriter error)
    {
        var command = new Command("create");

        command.Add(new Argument<string>("label-or-url")
        {
            Arity = ArgumentArity.ExactlyOne,
        });
        command.Add(new Option<string>("--secret")
        {
            Arity = ArgumentArity.ZeroOrOne,
        });
        command.Add(new Option<int>("--period")
        {
            Arity = ArgumentArity.ZeroOrOne,
        });
        command.Add(new Option<string>("--algorithm")
        {
            Arity = ArgumentArity.ZeroOrOne,
        });
        command.Add(new Option<string>("--issuer")
        {
            Arity = ArgumentArity.ZeroOrOne,
        });

        return ConfigurePlaceholder(command, error);
    }

    private static Command CreateSingleLabelCommand(string name, TextWriter error)
    {
        var command = new Command(name);

        command.Add(new Argument<string>("label")
        {
            Arity = ArgumentArity.ExactlyOne,
        });

        return ConfigurePlaceholder(command, error);
    }

    private static Command CreateDeleteCommand(TextWriter error)
    {
        var command = new Command("delete");

        command.Add(new Argument<string[]>("labels")
        {
            Arity = ArgumentArity.OneOrMore,
        });

        return ConfigurePlaceholder(command, error);
    }

    private static Command CreatePlaceholderCommand(string name, TextWriter error)
    {
        return ConfigurePlaceholder(new Command(name), error);
    }

    private static Command ConfigurePlaceholder(Command command, TextWriter error)
    {
        command.SetAction(_ =>
        {
            error.WriteLine($"otp {command.Name} is not implemented.");
            return CliExitCodes.NotImplemented;
        });

        return command;
    }
}
