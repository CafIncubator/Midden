using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Models.v0_2.DataDictionary;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caf.Midden.Core.Services
{
    public class CafCsvMap : ClassMap<DataDictionaryRecordCafCsv>
    {
        public CafCsvMap()
        {
            Map(m => m.Name);
            Map(m => m.Description);
            Map(m => m.Units);
            Map(m => m.Tags).Optional();
            Map(m => m.Methods).Optional();
            Map(m => m.TemporalResolution).Optional();
            Map(m => m.TemporalExtent).Optional();
            Map(m => m.SpatialRepeats).Optional();
            Map(m => m.IsQCSpecified).Optional();
            Map(m => m.QCApplied).Optional();
            Map(m => m.ProcessingLevel).Optional();
            Map(m => m.VariableType).Optional();
        }
    }

    public class DataDictionaryReaderCafCsv : IReadDataDictionary
    {
        /// <summary>
        /// Delimiter used within a single CSV cell to separate multiple values
        /// for list-type fields (Tags, Methods, QCApplied). This must match the
        /// delimiter used when generating the downloadable CSV.
        /// </summary>
        public const string ListValueDelimiter = ";";

        public List<Variable> Read(string path)
        {
            throw new NotImplementedException();
        }

        public List<Variable> Read(
            Stream stream)
        {
            List<Variable> variables = new List<Variable>();

            CsvConfiguration configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Detects rows whose parsed column count differs from the header,
                // which typically means a value containing a comma was not quoted
                // (e.g. an unquoted "Raw, QC'd" in ProcessingLevel). Without this,
                // such rows silently shift values into the wrong columns instead
                // of failing.
                DetectColumnCountChanges = true
            };

            List<DataDictionaryRecordCafCsv> records;

            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var csv = new CsvReader(reader, configuration))
            {
                csv.Context.RegisterClassMap<CafCsvMap>();

                try
                {
                    records = csv.GetRecords<DataDictionaryRecordCafCsv>().ToList();
                }
                catch (BadDataException ex)
                {
                    int row = ex.Context?.Parser?.Row ?? -1;
                    throw new DataDictionaryReadException(new List<string>
                    {
                        $"Row {row}: The number of columns does not match the header row. " +
                        "This usually means a value containing a comma was not wrapped in double quotes."
                    });
                }
                catch (HeaderValidationException)
                {
                    throw new DataDictionaryReadException(new List<string>
                    {
                        "The CSV is missing one or more required columns: Name, Description, Units."
                    });
                }

                List<string> rowErrors = new List<string>();

                for (int i = 0; i < records.Count; i++)
                {
                    DataDictionaryRecordCafCsv record = records[i];
                    int rowNumber = i + 2; // account for header row, 1-based

                    if (string.IsNullOrWhiteSpace(record.Name))
                    {
                        rowErrors.Add($"Row {rowNumber}: Name is required.");
                        continue;
                    }

                    int? spatialRepeats = null;
                    if (!string.IsNullOrWhiteSpace(record.SpatialRepeats))
                    {
                        if (int.TryParse(record.SpatialRepeats, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedSpatialRepeats))
                        {
                            spatialRepeats = parsedSpatialRepeats;
                        }
                        else
                        {
                            rowErrors.Add($"Row {rowNumber}: SpatialRepeats \"{record.SpatialRepeats}\" is not a valid integer.");
                        }
                    }

                    bool? isQCSpecified = null;
                    if (!string.IsNullOrWhiteSpace(record.IsQCSpecified))
                    {
                        if (bool.TryParse(record.IsQCSpecified, out bool parsedIsQCSpecified))
                        {
                            isQCSpecified = parsedIsQCSpecified;
                        }
                        else
                        {
                            rowErrors.Add($"Row {rowNumber}: IsQCSpecified \"{record.IsQCSpecified}\" is not a valid true/false value.");
                        }
                    }

                    variables.Add(new Variable()
                    {
                        Name = record.Name,
                        Description = record.Description,
                        Units = record.Units,
                        Tags = SplitListValue(record.Tags),
                        Methods = SplitListValue(record.Methods),
                        TemporalResolution = record.TemporalResolution,
                        TemporalExtent = record.TemporalExtent,
                        SpatialRepeats = spatialRepeats,
                        IsQCSpecified = isQCSpecified,
                        QCApplied = SplitListValue(record.QCApplied),
                        ProcessingLevel = record.ProcessingLevel,
                        VariableType = record.VariableType
                    });
                }

                if (rowErrors.Count > 0)
                {
                    throw new DataDictionaryReadException(rowErrors);
                }
            }

            return variables;
        }

        private static List<string> SplitListValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            return value
                .Split(ListValueDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }

    /// <summary>
    /// Thrown when one or more rows in an uploaded data dictionary CSV fail to parse.
    /// </summary>
    public class DataDictionaryReadException : Exception
    {
        public IReadOnlyList<string> RowErrors { get; }

        public DataDictionaryReadException(IReadOnlyList<string> rowErrors)
            : base("One or more rows in the data dictionary CSV could not be read:\n" + string.Join("\n", rowErrors))
        {
            RowErrors = rowErrors;
        }
    }
}
