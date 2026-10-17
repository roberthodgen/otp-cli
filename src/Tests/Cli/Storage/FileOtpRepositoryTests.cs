using Shouldly;
using Xunit;

namespace RobertHodgen.Otp.Cli.Tests.Storage;

public sealed class FileOtpRepositoryTests
{
    [Fact]
    public void Create_writes_utf8_uri_at_encoded_path_and_list_decodes_it()
    {
        using var root = new TemporaryRoot();
        var repository = new FileOtpRepository(root.Path);
        var label = OtpLabel.Parse("example.com:rob");

        repository.CreateIfAbsent(label, "otpauth://totp/example.com%3Arob?secret=JBSWY3DP").ShouldBeTrue();

        File.ReadAllText(Path.Combine(root.Path, "example.com%3Arob"))
            .ShouldBe("otpauth://totp/example.com%3Arob?secret=JBSWY3DP");
        repository.List().Select(item => item.Value).ShouldBe(["example.com:rob"]);
    }

    [Fact]
    public void Missing_root_lists_as_empty_and_get_and_delete_are_no_ops()
    {
        using var root = new TemporaryRoot(createImmediately: false);
        var repository = new FileOtpRepository(root.Path);
        var label = OtpLabel.Parse("example.com");

        repository.List().ShouldBeEmpty();
        repository.Get(label).ShouldBeNull();
        Should.NotThrow(() => repository.DeleteIfPresent(label));
    }

    [Fact]
    public void Duplicate_create_preserves_the_original_uri()
    {
        using var root = new TemporaryRoot();
        var repository = new FileOtpRepository(root.Path);
        var label = OtpLabel.Parse("example.com");

        repository.CreateIfAbsent(label, "first").ShouldBeTrue();
        repository.CreateIfAbsent(label, "second").ShouldBeFalse();

        repository.Get(label).ShouldBe("first");
    }

    [Fact]
    public void List_sorts_decoded_labels_ordinally()
    {
        using var root = new TemporaryRoot();
        var repository = new FileOtpRepository(root.Path);

        repository.CreateIfAbsent(OtpLabel.Parse("zebra"), "zebra");
        repository.CreateIfAbsent(OtpLabel.Parse("Alpha"), "alpha");

        repository.List().Select(item => item.Value).ShouldBe(["Alpha", "zebra"]);
    }

    [Fact]
    public void FromEnvironment_uses_nonempty_override_directory()
    {
        using var root = new TemporaryRoot();
        using var environment = new EnvironmentVariableScope("OTP_CLI_OVERRIDE_DIR", root.Path);

        var repository = FileOtpRepository.FromEnvironment();
        var label = OtpLabel.Parse("example.com");

        repository.CreateIfAbsent(label, "value").ShouldBeTrue();
        File.ReadAllText(Path.Combine(root.Path, "example.com")).ShouldBe("value");
    }

    private sealed class TemporaryRoot : IDisposable
    {
        public TemporaryRoot(bool createImmediately = true)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            if (createImmediately)
            {
                Directory.CreateDirectory(Path);
            }
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string _name;
        private readonly string? _previousValue;

        public EnvironmentVariableScope(string name, string value)
        {
            _name = name;
            _previousValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose() => Environment.SetEnvironmentVariable(_name, _previousValue);
    }
}
