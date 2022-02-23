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
    public class MetadataParserTest
    {
        [Fact]
        private void Parse_v0_1_0alpha1String_ExpectedBehavior()
        {
            string filePath = @"Assets\MetadataFiles\example_v0_1_0alpha1.json";
            string json = File.ReadAllText(filePath);
            var sut = new MetadataParser(
                new MetadataConverter());

            var actual = sut.Parse(json);

            Assert.IsType<Models.v0_2.Metadata>(actual);
        }
    }
}
