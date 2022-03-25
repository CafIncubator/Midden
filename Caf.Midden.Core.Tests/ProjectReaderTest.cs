using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using System;
using System.Collections.Generic;
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
        private void ReadAsync_v0_2_NoFrontMatter_ReturnsNull()
        {
            string filePath = @"Assets\ProjectFiles\v0_2_no_front_matter.md";
            ProjectParser parser = new ProjectParser();
            ProjectReader sut = new ProjectReader(parser);

            Models.v0_2.Project actual;
            using (Stream stream = File.OpenRead(filePath))
            {
                actual = sut.Read(stream);
            }

            Assert.Null(actual);
        }

        [Fact]
        private void ReadAsync_v0_2_IncorrectFrontMatter_ReturnsNull()
        {
            string filePath = @"Assets\ProjectFiles\v0_2_incorrect_front_matter.md";
            ProjectParser parser = new ProjectParser();
            ProjectReader sut = new ProjectReader(parser);

            Models.v0_2.Project actual;
            using (Stream stream = File.OpenRead(filePath))
            {
                actual = sut.Read(stream);
            }

            Assert.Null(actual);
        }

        [Fact]
        private void ReadAsync_v0_2_NoClosingFrontMatter_ReturnsNull()
        {
            string filePath = @"Assets\ProjectFiles\v0_2_incorrect_front_matter.md";
            ProjectParser parser = new ProjectParser();
            ProjectReader sut = new ProjectReader(parser);

            Models.v0_2.Project actual;
            using (Stream stream = File.OpenRead(filePath))
            {
                actual = sut.Read(stream);
            }

            Assert.Null(actual);
        }

        [Fact]
        private void ReadAsync_v0_2_CorrectFrontMatter_ReturnsNull()
        {
            string filePath = @"Assets\ProjectFiles\v0_2_correct_front_matter.md";
            ProjectParser parser = new ProjectParser();
            ProjectReader sut = new ProjectReader(parser);

            Models.v0_2.Project actual;
            using (Stream stream = File.OpenRead(filePath))
            {
                actual = sut.Read(stream);
            }

            Assert.NotNull(actual);
            Assert.Equal("TestProject", actual.Name);
            Assert.Equal("# Heading", actual.Description);
        }

        [Fact]
        private void ReadAsync_v0_2_CorrectFrontMatterWithAdditonalVariables_ReturnsNull()
        {
            string filePath = @"Assets\ProjectFiles\v0_2_correct_front_matter_additional_variables.md";
            ProjectParser parser = new ProjectParser();
            ProjectReader sut = new ProjectReader(parser);

            Models.v0_2.Project actual;
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
