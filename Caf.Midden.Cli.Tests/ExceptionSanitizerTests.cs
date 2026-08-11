using Caf.Midden.Cli.Common;

namespace Caf.Midden.Cli.Tests;

public class ExceptionSanitizerTests
{
    [Fact]
    public void Describe_NotVerbose_RedactsQueryString()
    {
        var exception = new InvalidOperationException(
            "Request to https://account.file.core.windows.net/share/file.midden?sv=2021-08-06&sig=SECRETVALUE failed.");

        var actual = ExceptionSanitizer.Describe(exception, verbose: false);

        Assert.DoesNotContain("SECRETVALUE", actual);
        Assert.DoesNotContain("sig=", actual);
        Assert.Contains("?<redacted>", actual);
    }

    [Fact]
    public void Describe_Verbose_ReturnsFullExceptionDetail()
    {
        var exception = new InvalidOperationException(
            "Request to https://account.file.core.windows.net/share/file.midden?sv=2021-08-06&sig=SECRETVALUE failed.");

        var actual = ExceptionSanitizer.Describe(exception, verbose: true);

        Assert.Contains("SECRETVALUE", actual);
    }

    [Fact]
    public void Describe_NotVerbose_NoQueryString_MessageUnchanged()
    {
        var exception = new InvalidOperationException("Data store 'foo' could not be reached.");

        var actual = ExceptionSanitizer.Describe(exception, verbose: false);

        Assert.Equal(exception.Message, actual);
    }
}
