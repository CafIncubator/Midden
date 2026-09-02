using Caf.Midden.Core.Models.v0_2;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Caf.Midden.Core.Services;

public static class DataDictionaryWriterCafCsv
{
    private sealed record CsvColumn(string Header, Func<Variable, Dataset, string?> GetValue);

    private static readonly CsvColumn[] Columns =
    [
        new(nameof(Variable.Name), (variable, _) => variable.Name),
        new(nameof(Variable.Description), (variable, _) => variable.Description),
        new(nameof(Variable.Units), (variable, _) => variable.Units),
        new(nameof(Variable.Tags), (variable, dataset) => FormatList(variable.Tags, dataset.Tags)),
        new(nameof(Variable.Methods), (variable, dataset) => FormatList(variable.Methods, dataset.Methods)),
        new(nameof(Variable.TemporalResolution), (variable, dataset) => Inherit(variable.TemporalResolution, dataset.TemporalResolution)),
        new(nameof(Variable.TemporalExtent), (variable, dataset) => InheritTemporalExtent(variable.TemporalExtent, dataset.TemporalExtent)),
        new(nameof(Variable.SpatialRepeats), (variable, dataset) => (variable.SpatialRepeats ?? dataset.SpatialRepeats)?.ToString(CultureInfo.InvariantCulture)),
        new(nameof(Variable.QCApplied), (variable, _) => FormatList(variable.QCApplied)),
        new(nameof(Variable.ProcessingLevel), (variable, _) => variable.ProcessingLevel),
        new(nameof(Variable.VariableType), (variable, _) => variable.VariableType)
    ];

    public static string Write(Dataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using (var csv = new CsvWriter(stringWriter, CultureInfo.InvariantCulture))
        {
            foreach (CsvColumn column in Columns)
            {
                csv.WriteField(column.Header);
            }

            csv.NextRecord();

            foreach (Variable variable in dataset.Variables ?? [])
            {
                foreach (CsvColumn column in Columns)
                {
                    csv.WriteField(column.GetValue(variable, dataset));
                }

                csv.NextRecord();
            }
        }

        return stringWriter.ToString();
    }

    private static string? Inherit(string? value, string? parentValue)
    {
        return string.IsNullOrWhiteSpace(value) ? parentValue : value;
    }

    private static string? InheritTemporalExtent(string? value, string? parentValue)
    {
        return string.IsNullOrWhiteSpace(value) || value.Trim() == "/" ? parentValue : value;
    }

    private static string FormatList(
        IReadOnlyCollection<string>? values,
        IReadOnlyCollection<string>? parentValues = null)
    {
        List<string> meaningfulValues = GetMeaningfulValues(values);
        if (meaningfulValues.Count == 0)
        {
            meaningfulValues = GetMeaningfulValues(parentValues);
        }

        return string.Join($"{DataDictionaryReaderCafCsv.ListValueDelimiter} ", meaningfulValues);
    }

    private static List<string> GetMeaningfulValues(IReadOnlyCollection<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToList() ?? [];
    }
}
