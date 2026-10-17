using System.CommandLine;

namespace RobertHodgen.Otp.Cli;

public static class CliApplication
{
    public static RootCommand CreateRootCommand(TextWriter? error = null)
    {
        _ = error ?? Console.Error;

        var rootCommand = new RootCommand();

        rootCommand.Add(new Command("list"));
        rootCommand.Add(CreateCreateCommand());
        rootCommand.Add(CreateSingleLabelCommand("read"));
        rootCommand.Add(CreateSingleLabelCommand("copy"));
        rootCommand.Add(CreateDeleteCommand());

        return rootCommand;
    }

    private static Command CreateCreateCommand()
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

        return command;
    }

    private static Command CreateSingleLabelCommand(string name)
    {
        var command = new Command(name);

        command.Add(new Argument<string>("label")
        {
            Arity = ArgumentArity.ExactlyOne,
        });

        return command;
    }

    private static Command CreateDeleteCommand()
    {
        var command = new Command("delete");

        command.Add(new Argument<string[]>("labels")
        {
            Arity = ArgumentArity.OneOrMore,
        });

        return command;
    }
}
