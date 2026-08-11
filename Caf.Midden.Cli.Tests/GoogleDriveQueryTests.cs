using Caf.Midden.Cli.Common;

namespace Caf.Midden.Cli.Tests;

public class GoogleDriveQueryTests
{
    [Fact]
    public void EscapeTerm_ApostropheInTerm_IsEscaped()
    {
        var actual = GoogleDriveQuery.EscapeTerm("O'Brien.midden");

        Assert.Equal(@"O\'Brien.midden", actual);
    }

    [Fact]
    public void EscapeTerm_BackslashInTerm_IsEscaped()
    {
        var actual = GoogleDriveQuery.EscapeTerm(@"Raw\CookEast.midden");

        Assert.Equal(@"Raw\\CookEast.midden", actual);
    }

    [Fact]
    public void EscapeTerm_NoSpecialCharacters_IsUnchanged()
    {
        var actual = GoogleDriveQuery.EscapeTerm("CookEastMet.midden");

        Assert.Equal("CookEastMet.midden", actual);
    }
}
