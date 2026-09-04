using System.Globalization;

namespace Caf.Midden.Core.Services.Validation;

/// <summary>
/// Why a candidate temporal extent string was rejected.
/// </summary>
public enum TemporalExtentStatus
{
    Valid,

    /// <summary>No <c>/</c> separating the start from the end, or more than one.</summary>
    MissingSeparator,

    /// <summary>The portion before the separator is not an ISO 8601 date/time.</summary>
    InvalidStart,

    /// <summary>The portion after the separator is not an ISO 8601 date/time.</summary>
    InvalidEnd,

    /// <summary>Both ends parsed, but the interval runs backwards.</summary>
    EndBeforeStart
}

/// <summary>
/// Parses <see cref="Models.v0_2.Dataset.TemporalExtent"/>, which is free text documented as an
/// ISO 8601 time interval in the form <c>{start}/{end}</c> (e.g. <c>2011-01-01/2019-10-30</c>).
/// </summary>
/// <remarks>
/// Nothing in the codebase parsed this before, so every catalog consumer has been trusting a
/// hand-typed string. Open-ended intervals (a trailing or leading empty side) are accepted, since
/// "collection is ongoing" is a legitimate thing for a researcher to express.
///
/// ISO 8601 reduced precision is also accepted, so <c>2017</c> and <c>2017-06</c> are valid bounds.
/// A researcher who knows the year but not the month should be able to say exactly that rather
/// than inventing a January 1st that implies precision the data does not have.
/// </remarks>
public static class TemporalExtentValidator
{
    public static TemporalExtentStatus Validate(string? temporalExtent) =>
        Validate(temporalExtent, out _, out _);

    /// <summary>
    /// Validates <paramref name="temporalExtent"/> and, when possible, yields the parsed bounds.
    /// Either bound may be null for an open-ended interval.
    /// </summary>
    /// <remarks>
    /// Reduced-precision bounds are widened to the period they name: <c>2017</c> as a start is
    /// 2017-01-01, and as an end is the last instant of 2017. Collapsing both to January 1st would
    /// make the legitimate <c>2017-06/2017</c> look like it ran backwards.
    /// </remarks>
    public static TemporalExtentStatus Validate(
        string? temporalExtent,
        out DateTimeOffset? start,
        out DateTimeOffset? end)
    {
        start = null;
        end = null;

        // Absent is not the same as malformed; the extent is optional.
        if (string.IsNullOrWhiteSpace(temporalExtent))
        {
            return TemporalExtentStatus.Valid;
        }

        var parts = temporalExtent.Split('/');

        if (parts.Length != 2)
        {
            return TemporalExtentStatus.MissingSeparator;
        }

        var startText = parts[0].Trim();
        var endText = parts[1].Trim();

        // An interval that is open at both ends carries no information, so it is treated as an
        // unusable value rather than as a vacuously valid one.
        if (startText.Length == 0 && endText.Length == 0)
        {
            return TemporalExtentStatus.InvalidStart;
        }

        if (startText.Length > 0)
        {
            if (!TryParseIso8601(startText, out var parsedStart, out _))
            {
                return TemporalExtentStatus.InvalidStart;
            }

            start = parsedStart;
        }

        if (endText.Length > 0)
        {
            if (!TryParseIso8601(endText, out var parsedEnd, out var endPrecision))
            {
                start = null;
                return TemporalExtentStatus.InvalidEnd;
            }

            end = EndOfPeriod(parsedEnd, endPrecision);
        }

        if (start.HasValue && end.HasValue && end.Value < start.Value)
        {
            return TemporalExtentStatus.EndBeforeStart;
        }

        return TemporalExtentStatus.Valid;
    }

    /// <summary>
    /// How much of a bound was actually specified.
    /// </summary>
    private enum Iso8601Precision
    {
        Year,
        Month,
        DayOrFiner
    }

    private static bool TryParseIso8601(
        string value,
        out DateTimeOffset parsed,
        out Iso8601Precision precision)
    {
        // A bare year is a valid ISO 8601 date but DateTimeOffset.TryParse rejects it outright,
        // so it is expanded before parsing rather than special-cased afterwards.
        if (value.Length == 4 && value.All(char.IsAsciiDigit))
        {
            precision = Iso8601Precision.Year;
            return TryParseExpanded($"{value}-01-01", out parsed);
        }

        // "2017-06" parses natively, but as a day-precision value, so the precision is recorded
        // here to widen it correctly when it is the end of an interval.
        precision = value.Length == 7 && value[4] == '-'
            ? Iso8601Precision.Month
            : Iso8601Precision.DayOrFiner;

        return TryParseExpanded(value, out parsed);
    }

    private static bool TryParseExpanded(string value, out DateTimeOffset parsed) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind | DateTimeStyles.AssumeUniversal,
            out parsed);

    private static DateTimeOffset EndOfPeriod(DateTimeOffset value, Iso8601Precision precision) =>
        precision switch
        {
            Iso8601Precision.Year => value.AddYears(1).AddTicks(-1),
            Iso8601Precision.Month => value.AddMonths(1).AddTicks(-1),
            _ => value
        };

    /// <summary>
    /// A researcher-facing explanation of <paramref name="status"/>, or null when it is valid.
    /// </summary>
    public static string? DescribeProblem(TemporalExtentStatus status) => status switch
    {
        TemporalExtentStatus.Valid => null,
        TemporalExtentStatus.MissingSeparator => "A temporal extent needs a start and an end separated by \"/\".",
        TemporalExtentStatus.InvalidStart => "The start of the temporal extent is not a recognizable date.",
        TemporalExtentStatus.InvalidEnd => "The end of the temporal extent is not a recognizable date.",
        TemporalExtentStatus.EndBeforeStart => "The temporal extent ends before it starts.",
        _ => "The temporal extent could not be understood."
    };

    /// <summary>
    /// How to fix <paramref name="status"/>, or null when there is nothing useful to say.
    /// </summary>
    public static string? DescribeFix(TemporalExtentStatus status) => status switch
    {
        TemporalExtentStatus.EndBeforeStart => "Swap the two dates.",
        TemporalExtentStatus.Valid => null,
        _ => "Use ISO 8601 dates, e.g. \"2011-01-01/2019-10-30\". A year or year-month alone is fine when the exact day is unknown, e.g. \"2017/2017\". Leave one side blank if collection is ongoing."
    };
}
