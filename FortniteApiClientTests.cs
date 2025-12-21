using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using RichardSzalay.MockHttp;
using Xunit;
using Fortnite_Replay_Parser_GUI.Services;

namespace Fortnite_Replay_Parser_GUI.Tests
{
    public class FortniteApiClientTests
    {
        [Fact]
        public async Task GetCosmeticsRawAsync_ReturnsExpectedJson()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            var expectedJson = "{\"data\":[{\"id\":\"item1\"}]}";
            mockHttp.When("https://fortnite-api.com/v2/cosmetics?language=en")
                    .Respond("application/json", expectedJson);

            var client = new HttpClient(mockHttp) { BaseAddress = new Uri("https://fortnite-api.com/v2/") };
            var api = new FortniteApiClient(client, disposeHttpClient: true);

            // Act
            var result = await api.GetCosmeticsRawAsync("en");

            // Assert
            Assert.Equal(expectedJson, result);
        }

        [Fact]
        public async Task SearchCosmeticsByIdsRawAsync_MultipleIds_ConstructsQueryAndReturnsJson()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            var expectedJson = "{\"data\":[{\"id\":\"qqqq\"},{\"id\":\"wwee\"}]}";
            var expectedRequest = "https://fortnite-api.com/v2/cosmetics/br/search/ids?language=en&id=qqqq&id=wwee";

            mockHttp.When(expectedRequest)
                    .Respond("application/json", expectedJson);

            var client = new HttpClient(mockHttp) { BaseAddress = new Uri("https://fortnite-api.com/v2/") };
            var api = new FortniteApiClient(client, disposeHttpClient: true);

            // Act
            var result = await api.SearchCosmeticsByIdsAsync(new List<string> { "qqqq", "wwee" }, "en");

            // Assert
            Assert.Equal(expectedJson, result);
        }

        [Fact]
        public async Task GetCosmeticsRawAsync_InvalidLanguage_ThrowsArgumentException()
        {
            var mockHttp = new MockHttpMessageHandler();
            var client = new HttpClient(mockHttp) { BaseAddress = new Uri("https://fortnite-api.com/v2/") };
            var api = new FortniteApiClient(client, disposeHttpClient: true);

            await Assert.ThrowsAsync<ArgumentException>(() => api.GetCosmeticsRawAsync(""));
        }

        [Fact]
        public async Task SearchCosmeticsByIdsRawAsync_NoIds_ThrowsArgumentException()
        {
            var mockHttp = new MockHttpMessageHandler();
            var client = new HttpClient(mockHttp) { BaseAddress = new Uri("https://fortnite-api.com/v2/") };
            var api = new FortniteApiClient(client, disposeHttpClient: true);

            await Assert.ThrowsAsync<ArgumentException>(() => api.SearchCosmeticsByIdsAsync(new List<string>(), "en"));
        }
    }
}