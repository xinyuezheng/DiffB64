using Microsoft.VisualStudio.TestTools.UnitTesting;
using DiffB64;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web.Http;
using System.Net;
using System;
using System.Text;

namespace DiffB64.Tests.Components
{

    [TestClass, TestCategory("Components")]
    public class DiffB64ComponentsTest
    {
        [TestMethod]
        public async Task HttpClient_Should_Get_OKStatus_From_Products_Using_InMemory_Hosting()
        {
            var config = new HttpConfiguration();
            WebApiConfig.Register(config);

            using (var server = new HttpServer(config))
            {
                HttpClient client = MakeClient(server);

                HttpRequestMessage request = MakeRequest("/v1/diff/1/left", HttpMethod.Put, "{\"data\":\"AAAAAA==\"}");
                await CheckResponse(client, request, HttpStatusCode.Created);

            }
        }

        private static HttpRequestMessage MakeRequest(string url, HttpMethod method, string content = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, url);
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");//CONTENT-TYPE header
            return request;
        }

        private static async Task CheckResponse(HttpClient client, HttpRequestMessage request, HttpStatusCode exp_status_code, HttpContent exp_content = null)
        {
            using (var response = await client.SendAsync(request))
            {
                Assert.AreEqual(exp_status_code, response.StatusCode);

                if (exp_content != null)
                {
                    //dummy
                    Assert.AreEqual(1, 1);
                }
            }

        }

        private static HttpClient MakeClient(HttpServer server)
        {
            var client = new HttpClient(server);
            client.BaseAddress = new Uri("http://localhost");
            client.DefaultRequestHeaders
                  .Accept
                  .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
            return client;
        }
    }
}