using System.CommandLine;

namespace RobertHodgen.Otp.Cli;

using Application;
using Clipboard;
using Storage;
using Time;

public static class CliApplication
{
    public static RootCommand CreateRootCommand(TextWriter? error = null)
    {
        var dependencies = new CliDependencies(
            new OtpCommandService(FileOtpRepository.FromEnvironment(), new RunningSystemClock(), new TextCopyOtpClipboard()),
            Console.Out,
            error ?? Console.Error);

        return CreateRootCommand(dependencies);
    }

    public static RootCommand CreateRootCommand(CliDependencies dependencies)
    {
        var rootCommand = new RootCommand();

        rootCommand.Add(CreateListCommand(dependencies));
        rootCommand.Add(CreateCreateCommand(dependencies));
        rootCommand.Add(CreateSingleLabelCommand("read", dependencies, dependencies.Service.Read));
        rootCommand.Add(CreateSingleLabelCommand("copy", dependencies, dependencies.Service.Copy));
        rootCommand.Add(CreateDeleteCommand(dependencies));

        return rootCommand;
    }

    private static Command CreateCreateCommand(CliDependencies dependencies)
    {
        var command = new Command("create");
        var labelOrUrl = new Argument<string>("label-or-url")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
        var secret = new Option<string>("--secret")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
        var period = new Option<int>("--period")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
        var algorithm = new Option<string>("--algorithm")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
        var issuer = new Option<string>("--issuer")
        {
            Arity = ArgumentArity.ExactlyOne,
        };
        command.Add(labelOrUrl);
        command.Add(secret);
        command.Add(period);
        command.Add(algorithm);
        command.Add(issuer);
        command.SetAction(result => WriteOutcome(dependencies, dependencies.Service.Create(
            result.GetValue(labelOrUrl) ?? string.Empty,
            result.GetValue(secret),
            result.GetResult(period) is null ? null : result.GetValue(period),
            result.GetValue(algorithm),
            result.GetValue(issuer))));
        return command;
    }

    private static Command CreateSingleLabelCommand(string name, CliDependencies dependencies, Func<string, OtpCommandOutcome> action)
    {
        var command = new Command(name);
        var label = new Argument<string>("label")
        {
            Arity = ArgumentArity.ExactlyOne,
        };

        command.Add(label);

        command.SetAction(result => WriteOutcome(dependencies, action(result.GetValue(label) ?? string.Empty)));
        return command;
    }

    private static Command CreateDeleteCommand(CliDependencies dependencies)
    {
        var command = new Command("delete");
        var labels = new Argument<string[]>("labels")
        {
            Arity = ArgumentArity.OneOrMore,
        };

        command.Add(labels);

        command.SetAction(result => WriteOutcome(dependencies, dependencies.Service.Delete(result.GetValue(labels) ?? [])));
        return command;
    }

    private static Command CreateListCommand(CliDependencies dependencies)
    {
        var command = new Command("list");
        command.SetAction(_ => WriteOutcome(dependencies, dependencies.Service.List()));
        return command;
    }

    private static int WriteOutcome(CliDependencies dependencies, OtpCommandOutcome outcome)
    {
        if (outcome.StandardOutput is not null)
        {
            dependencies.Output.Write(outcome.StandardOutput);
        }

        if (outcome.StandardError is not null)
        {
            dependencies.Error.Write(outcome.StandardError);
        }

        return outcome.ExitCode;
    }
}
