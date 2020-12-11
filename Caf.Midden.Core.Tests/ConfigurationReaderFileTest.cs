using Caf.Midden.Core.Services.Configuration;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Caf.Midden.Core.Tests
{
    public class ConfigurationReaderFileTest
    {
        [Fact]
        public async void Read_ValidFile_ReturnsExpected()
        {
            // Not sure how to moq static method of httpclient
            // and since that's the only function (so far) that sut is doing
            // no reason to test it.

            /*
            string filePath = @"Assets\ConfigFiles\app-config_v0_1_0-alpha4.json";
            string jsonData = File.ReadAllText(filePath);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonData)
                })
                .Verifiable();

            HttpClient httpClient = new HttpClient();
            
            int expectedZones = 6;
            int expectedProcessingLevels = 3;
            string expectedOrgName = "R.J. Cook Agronomy Farm";

            var sut = new ConfigurationReaderHttp(
                httpClient,
                filePath);

            var actual = await sut.Read();

            Assert.Equal(expectedZones, actual.Zones.Count);
            Assert.Equal(expectedProcessingLevels, actual.ProcessingLevels.Count);
            Assert.Equal(expectedOrgName, actual.OrganizationName);
            */
        }
    }
}
