using Caf.Midden.Core.Services.Validation;
using Xunit;

namespace Caf.Midden.Core.Tests;

public class TemporalExtentValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_AbsentValue_IsValid(string? extent)
    {
        Assert.Equal(TemporalExtentStatus.Valid, TemporalExtentValidator.Validate(extent));
    }

    [Theory]
    [InlineData("2011-01-01/2019-10-30")]
    [InlineData("1997-07-16/1997-07-17")]
    [InlineData("2011-01-01T00:00:00Z/2019-10-30T12:30:00Z")]
    [InlineData(" 2011-01-01 / 2019-10-30 ")]
    public void Validate_Iso8601Interval_IsValid(string extent)
    {
        Assert.Equal(TemporalExtentStatus.Valid, TemporalExtentValidator.Validate(extent));
    }

    [Theory]
    [InlineData("2011-01-01/")]
    [InlineData("/2019-10-30")]
    public void Validate_OpenEndedInterval_IsValid(string extent)
    {
        // "Collection is ongoing" is a legitimate thing for a researcher to express.
        Assert.Equal(TemporalExtentStatus.Valid, TemporalExtentValidator.Validate(extent));
    }

    [Fact]
    public void Validate_IntervalOpenAtBothEnds_IsRejected()
    {
        Assert.Equal(TemporalExtentStatus.InvalidStart, TemporalExtentValidator.Validate("/"));
    }

    [Theory]
    [InlineData("2017/2017")]
    [InlineData("2011/2019")]
    [InlineData("2017-06/2017-08")]
    [InlineData("2017-06/2017")]
    [InlineData("2017/2017-06")]
    [InlineData("2017-06-15/2017")]
    public void Validate_ReducedPrecisionBounds_AreValid(string extent)
    {
        // ISO 8601 permits reduced precision, and "we know the year but not the month" is a real
        // state of knowledge that should not be forced into a fabricated January 1st.
        Assert.Equal(TemporalExtentStatus.Valid, TemporalExtentValidator.Validate(extent));
    }

    [Fact]
    public void Validate_ReducedPrecisionEnd_CoversTheWholePeriod()
    {
        // A year-only end means "through the end of that year", otherwise a collection that ran
        // into December would appear to end before it started.
        var status = TemporalExtentValidator.Validate("2017/2017", out var start, out var end);

        Assert.Equal(TemporalExtentStatus.Valid, status);
        Assert.Equal(new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(2017, end!.Value.Year);
        Assert.Equal(12, end.Value.Month);
        Assert.Equal(31, end.Value.Day);
    }

    [Fact]
    public void Validate_ReducedPrecisionMonthEnd_CoversTheWholeMonth()
    {
        TemporalExtentValidator.Validate("2017-06/2017-06", out _, out var end);

        Assert.Equal(6, end!.Value.Month);
        Assert.Equal(30, end.Value.Day);
    }

    [Theory]
    [InlineData("2011-01-01", TemporalExtentStatus.MissingSeparator)]
    [InlineData("2011-01-01/2015-01-01/2019-01-01", TemporalExtentStatus.MissingSeparator)]
    [InlineData("last summer/2019-10-30", TemporalExtentStatus.InvalidStart)]
    [InlineData("2011-01-01/whenever", TemporalExtentStatus.InvalidEnd)]
    [InlineData("2019-10-30/2011-01-01", TemporalExtentStatus.EndBeforeStart)]
    [InlineData("2019/2011", TemporalExtentStatus.EndBeforeStart)]
    [InlineData("20177/2019", TemporalExtentStatus.InvalidStart)]
    public void Validate_MalformedInterval_ReportsSpecificStatus(
        string extent,
        TemporalExtentStatus expected)
    {
        Assert.Equal(expected, TemporalExtentValidator.Validate(extent));
    }

    [Fact]
    public void Validate_ValidInterval_YieldsParsedBounds()
    {
        var status = TemporalExtentValidator.Validate("2011-01-01/2019-10-30", out var start, out var end);

        Assert.Equal(TemporalExtentStatus.Valid, status);
        Assert.Equal(2011, start!.Value.Year);
        Assert.Equal(2019, end!.Value.Year);
    }

    [Fact]
    public void Validate_OpenEndedInterval_YieldsOnlyTheSpecifiedBound()
    {
        TemporalExtentValidator.Validate("2011-01-01/", out var start, out var end);

        Assert.NotNull(start);
        Assert.Null(end);
    }
}
