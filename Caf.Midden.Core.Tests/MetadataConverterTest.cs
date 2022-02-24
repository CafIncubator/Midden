using Caf.Midden.Core.Services.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Caf.Midden.Core.Tests
{
    public class MetadataConverterTest
    {
        [Fact]
        public void Convert_v0_1_0alpha3Nulls_ConvertsProperly()
        {
            Models.v0_1_0alpha3.Metadata input = 
                new Models.v0_1_0alpha3.Metadata();
            input.File.CreationDate = "2020-01-01";

            var sut = new MetadataConverter();

            var actual = sut.Convert(input);

            Assert.IsType<Models.v0_2.Metadata>(actual);
        }

        public void Convert_v0_1_0alpha3WithVals_ConvertsProperly()
        {
            var creationDate = "2020-12-20";
            var contactName = "TestUser";
            var startDate = "1999-20-01";
            var tempExtent = $"{startDate}/{startDate}";
            List<string> expectedQC = new List<string>()
            {
                "Assurance",
                "Point",
                "Review"
            };

            Models.v0_1_0alpha3.Metadata input =
                new Models.v0_1_0alpha3.Metadata()
                {
                    File = new Models.v0_1_0alpha3.File()
                    {
                        CreationDate = creationDate
                    },
                    Dataset = new Models.v0_1_0alpha3.Dataset()
                    {
                        Zone = Models.v0_1_0alpha3.Zones.Production,
                        Project = "UnitTest",
                        Name = "Convert_v0_1_0alpha3WithVals_ConvertsProperly",
                        Description = "Pleh",
                        Format = "geotiff",
                        Contacts = new List<Models.v0_1_0alpha3.Person>()
                        {
                            new Models.v0_1_0alpha3.Person()
                            {
                                Name = contactName,
                                Email = "test@email.com",
                                Role = "Tool"
                            }
                        },
                        StartDate = startDate,
                        EndDate = startDate,
                        Variables = new List<Models.v0_1_0alpha3.Variable>()
                        {
                            new Models.v0_1_0alpha3.Variable()
                            {
                                Name = "value",
                                IsQCSpecified = false,
                                ProcessingLevel = Models.v0_1_0alpha3.ProcessingLevels.Calculated
                            },
                            new Models.v0_1_0alpha3.Variable()
                            {
                                Name = "value2",
                                IsQCSpecified = true,
                                QCApplied = new Models.v0_1_0alpha3.QCApplied()
                                {
                                    Assurance = true,
                                    Point = true,
                                    Review = true
                                },
                                StartDate = startDate,
                                EndDate = startDate
                            }
                        }
                    }
                };

            var sut = new MetadataConverter();

            Models.v0_2.Metadata actual = sut.Convert(input);

            Assert.Equal(creationDate, actual.CreationDate.ToString());
            Assert.Equal(contactName, actual.Dataset.Contacts[0].Name);
            Assert.Equal(tempExtent, actual.Dataset.TemporalExtent);
            Assert.False(actual.Dataset.Variables[0].IsQCSpecified);
            Assert.Equal("Calculated", actual.Dataset.Variables[0].ProcessingLevel);
            Assert.Equal(expectedQC, actual.Dataset.Variables[1].QCApplied);
            Assert.Equal(tempExtent, actual.Dataset.Variables[1].TemporalExtent);
        }

//        [Fact]
//        public void Convert_v0_1_0alpha4_ConvertsProperly()
//        {
//            Models.v0_1_0alpha4.Metadata input =
//                new Models.v0_1_0alpha4.Metadata();
//
//            var sut = new MetadataConverter();
//
//            var actual = sut.Convert(input);
//
//            Assert.IsType<Models.v0_2.Metadata>(actual);
//        }
    }
}
