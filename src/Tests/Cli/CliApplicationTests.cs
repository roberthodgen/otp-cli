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
}
