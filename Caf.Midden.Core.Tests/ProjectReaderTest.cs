using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Caf.Midden.Core.Tests
{
    public class ProjectReaderTest
    {
        [Fact]
        public void Read_v0_2_NoFrontMatter_ReturnsNull()
        {
            string filePath = @"Assets\ProjectFiles\v0_2_no_front_matter.md";
            ProjectParser parser = new ProjectParser();
            ProjectReader sut = new ProjectReader(parser);

            Models.v0_2.Project? actual;
            using (Stream stream = File.OpenRead(filePath))
            {
                actual = sut.Read(stream);
            }

            Assert.Null(actual);
        }

        [Fact]
        public void Read_v0_2_IncorrectFrontMatter_ReturnsNull()
        {
            string filePath = @"Assets\ProjectFiles\v0_2_incorrect_front_matter.md";
            ProjectParser parser = new ProjectParser();
            ProjectReader sut = new ProjectReader(parser);

            Models.v0_2.Project? actual;
            using (Stream stream = File.OpenRead(filePath))
            {
                actual = sut.Read(stream);
            }

            Assert.Null(actual);
        }

        [Fact]
        public void Read_v0_2_NoClosingFrontMatter_ReturnsNull()
        {
            string filePath = @"Assets\ProjectFiles\v0_2_no_closing_front_matter.md";
            ProjectParser parser = new ProjectParser();
            ProjectReader sut = new ProjectReader(parser);

            Models.v0_2.Project? actual;
            using (Stream stream = File.OpenRead(filePath))
            {
                actual = sut.Read(stream);
            }

            Assert.Null(actual);
        }

        [Fact]
        public void Read_v0_2_CorrectFrontMatter_ReturnsProject()
        {
            string filePath = @"Assets\ProjectFiles\v0_2_correct_front_matter.md";
            ProjectParser parser = new ProjectParser();
            ProjectReader sut = new ProjectReader(parser);

            Models.v0_2.Project? actual;
            using (Stream stream = File.OpenRead(filePath))
            {
                actual = sut.Read(stream);
            }

            Assert.NotNull(actual);
            Assert.Equal("TestProject", actual.Name);
            Assert.Equal("# Heading", actual.Description);
            Assert.Equal(
                DateTime.Parse("2022-05-12T23:37:22.6390000Z", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                actual.LastModified);
            Assert.Equal("Incomplete", actual.ProjectStatus);
        }

        [Fact]
        public void Read_v0_2_CorrectFrontMatterWithAdditonalVariables_ReturnsProject()
        {
            string filePath = @"Assets\ProjectFiles\v0_2_correct_front_matter_additional_variables.md";
            ProjectParser parser = new ProjectParser();
            ProjectReader sut = new ProjectReader(parser);

            Models.v0_2.Project? actual;
            using (Stream stream = File.OpenRead(filePath))
            {
                actual = sut.Read(stream);
            }

            Assert.NotNull(actual);
            Assert.Equal("TestProject", actual.Name);
            Assert.Equal("# Heading", actual.Description);
        }
    }
}
