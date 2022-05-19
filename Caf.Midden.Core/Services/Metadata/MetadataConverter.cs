using System;
using System.Collections.Generic;

namespace Caf.Midden.Core.Services.Metadata
{
    public class MetadataConverter : IMetadataConverter
    {
        public Models.v0_2.Metadata Convert(
            Models.v0_1_0alpha3.Metadata metadata)
        {
            Models.v0_2.Metadata result =
                new Models.v0_2.Metadata
                {
                    CreationDate = DateTime.Parse(
                metadata.File.CreationDate),
                    ModifiedDate = DateTime.UtcNow
                };

            Models.v0_2.Dataset d;
            if(metadata.Dataset != null)
            {
                d = new Models.v0_2.Dataset()
                {
                    Zone = metadata.Dataset.Zone.ToString(),
                    Project = metadata.Dataset.Project,
                    Name = metadata.Dataset.Name,
                    Description = metadata.Dataset.Description,
                    Format = metadata.Dataset.Format,
                    FilePathTemplate = metadata.Dataset.FilePathTemplate,
                    FilePathDescriptor = metadata.Dataset.FilePathDescriptor,
                    Structure = metadata.Dataset.Structure,
                    Tags = metadata.Dataset.Tags,
                    Contacts = ConvertContacts(metadata.Dataset.Contacts),
                    Geometry = metadata.Dataset.Geometry,
                    Methods = metadata.Dataset.Methods,
                    TemporalResolution = metadata.Dataset.TemporalResolution,
                    TemporalExtent =
                    $"{metadata.Dataset.StartDate}/{metadata.Dataset.EndDate}",
                    SpatialRepeats = metadata.Dataset.SpatialRepeats,
                    Variables = ConvertVariables(metadata.Dataset.Variables)
                };
            }
            else { d = new Models.v0_2.Dataset(); }
            

            result.Dataset = d;


            return result;

        }

        public Models.v0_2.Metadata Convert(
            Models.v0_1_0alpha4.Metadata metadata)
        {
            // I'm hoping this never occurs, any v0_1_0-alpha4 schemas should be treated as v0_1
            throw new NotImplementedException();
        }

        public Models.v0_2.Metadata Convert(
            Models.v0_1.Metadata metadata)
        {
            // I think this shouldn't occur since any v0_1 should be treated as v0_2
            throw new NotImplementedException();
        }

        public Models.v0_2.Metadata Convert(
            Models.v0_2.Metadata metadata)
        {
            return metadata;
        }

        private List<Models.v0_2.Person> ConvertContacts(
            List<Models.v0_1_0alpha3.Person> contacts)
        {
            List<Models.v0_2.Person> result = 
                new List<Models.v0_2.Person>();

            foreach(var person in contacts)
            {
                result.Add(new Models.v0_2.Person()
                {
                    Name = person.Name,
                    Email = person.Email,
                    Role = person.Role
                });
            }

            return result;
        }

        private List<Models.v0_2.Variable> ConvertVariables(
            List<Models.v0_1_0alpha3.Variable> variables)
        {
            var result = new List<Models.v0_2.Variable>();

            foreach(var variable in variables)
            {
                var newVariable = new Models.v0_2.Variable()
                {
                    Name = variable.Name,
                    Description = variable.Description,
                    Units = variable.Units,
                    Height = variable.Height,
                    Tags = variable.Tags ??= new List<string>(),
                    Methods = variable.Methods ??= new List<string>(),
                    TemporalResolution = variable.TemporalResolution,
                    SpatialRepeats = variable.SpatialRepeats,
                    IsQCSpecified = variable.IsQCSpecified,
                    QCApplied = CopyQCApplied(
                        variable.QCApplied, 
                        variable.IsQCSpecified),
                    ProcessingLevel = variable.ProcessingLevel.ToString()
                };

                // Get temporal extent
                string temporalStart = string.IsNullOrEmpty(variable.StartDate) ? "" : variable.StartDate;
                string temporalEnd = string.IsNullOrEmpty(variable.EndDate) ? "" : variable.StartDate;

                newVariable.TemporalExtent = $"{temporalStart}/{temporalEnd}";

                result.Add(newVariable);
            }

            return result;
        }

        private List<string> CopyQCApplied(
            Models.v0_1_0alpha3.QCApplied qc, bool qcSpecified)
        {
            var result = new List<string>();

            if (qc == null)
            {
                if (qcSpecified)
                    result.Add("None");
                else { result.Add("Unknown"); }
            }
            else
            {
                if (qc.Assurance) result.Add("Assurance");
                if (qc.Point) result.Add("Point");
                if (qc.Observation) result.Add("Observation");
                if (qc.Dataset) result.Add("Dataset");
                if (qc.External) result.Add("External");
                if (qc.Review) result.Add("Review");
            }

            return result;
        }
    }
}
