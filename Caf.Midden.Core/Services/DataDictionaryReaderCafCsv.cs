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
            Map(m => m.FieldName);
            Map(m => m.Units);
            Map(m => m.Description);
            Map(m => m.DataType).Optional();
        }
    }
    public class DataDictionaryReaderCafCsv : IReadDataDictionary
    {
        public List<Variable> Read(string path)
        {
            throw new NotImplementedException();
        }

        public List<Variable> Read(
            Stream stream)
        {
            List<Variable> variables = new List<Variable>();

            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {

                csv.Context.RegisterClassMap<CafCsvMap>();
                List<DataDictionaryRecordCafCsv> records =
                    csv.GetRecords<DataDictionaryRecordCafCsv>().ToList();

                foreach (DataDictionaryRecordCafCsv record in records)
                {
                    variables.Add(new Variable()
                    {
                        Name = record.FieldName,
                        Description = record.Description,
                        Units = record.Units
                    });
                }
            }

            return variables;
        }
    }
}
