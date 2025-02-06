using Caf.Midden.Cli.Services;
using Caf.Midden.Core.Models.v0_2;
using Caf.Midden.Core.Services.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Caf.Midden.Cli.Tests
{
    public class LocalFileSystemCrawlerTests
    {
        [Fact]
        public void GetFileNames_ValidInput_ReturnsExpected()
        {
            var sut = new LocalFileSystemCrawler(@"Assets\MockDataStoreLocal");

            var actual = sut.GetFileNames(".midden");

            Assert.Equal(5, actual.Count);
        }

        [Fact]
        public void GetMetaDatas_ValidInput_ReturnsWithVariableType()
        {
            var sut = new LocalFileSystemCrawler(@"Assets\MockDataStoreLocalVarTypes");

            List<Metadata> actual = sut.GetMetadatas(new MetadataParser(new MetadataConverter()));

            Assert.NotNull(actual[0].Dataset.Variables[0].VariableType);
        }
    }
}
