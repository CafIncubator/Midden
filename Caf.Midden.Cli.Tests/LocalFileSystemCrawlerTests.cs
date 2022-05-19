using Caf.Midden.Cli.Services;
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
    }
}
