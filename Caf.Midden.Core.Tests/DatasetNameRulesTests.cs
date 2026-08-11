using Caf.Midden.Core.Services.Validation;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class DatasetNameRulesTests
{
    [Theory]
    [InlineData("CookEastMet")]
    [InlineData("Cook East Met 2019")]
    [InlineData("cook-east_met.v2")]
    public void Validate_UsableName_IsValid(string name)
    {
        Assert.Equal(DatasetNameStatus.Valid, DatasetNameRules.Validate(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingName_IsEmpty(string? name)
    {
        Assert.Equal(DatasetNameStatus.Empty, DatasetNameRules.Validate(name));
    }

    [Theory]
    [InlineData("Cook/East")]
    [InlineData("Cook\\East")]
    [InlineData("Cook:East")]
    [InlineData("Cook*East")]
    [InlineData("Cook?East")]
    [InlineData("Cook|East")]
    [InlineData("Cook\"East\"")]
    [InlineData("Cook<East>")]
    public void Validate_PathUnsafeName_IsRejected(string name)
    {
        // The name becomes "{Name}.midden" and the crawlers derive the dataset path from it, so an
        // unsafe name means Collate silently drops the dataset from the catalog.
        Assert.Equal(DatasetNameStatus.IllegalCharacter, DatasetNameRules.Validate(name));
    }

    [Theory]
    [InlineData(" CookEast")]
    [InlineData("CookEast ")]
    [InlineData(".CookEast")]
    [InlineData("CookEast.")]
    public void Validate_LeadingOrTrailingWhitespaceOrDot_IsRejected(string name)
    {
        Assert.Equal(
            DatasetNameStatus.LeadingOrTrailingWhitespaceOrDot,
            DatasetNameRules.Validate(name));
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("LPT1")]
    [InlineData("NUL.data")]
    public void Validate_WindowsReservedName_IsRejected(string name)
    {
        Assert.Equal(DatasetNameStatus.ReservedName, DatasetNameRules.Validate(name));
    }

    [Fact]
    public void Validate_ControlCharacter_IsRejected()
    {
        Assert.Equal(DatasetNameStatus.IllegalCharacter, DatasetNameRules.Validate("Cook\tEast"));
    }
}
