using Microsoft.VisualStudio.TestTools.UnitTesting;
using DiffB64;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web.Http;
using System.Net;
using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DiffB64.Tests.Components
{

    [TestClass, TestCategory("Components")]
    public class DiffB64ComponentsTest
    {
        [TestMethod]
        public async Task DiffB64HappyFlow()
        {
            var config = new HttpConfiguration();
            WebApiConfig.Register(config);

            using (var server = new HttpServer(config))
            {
                HttpClient client = MakeClient(server);
                HttpRequestMessage request;
                JObject exp_cont;

                request = MakeRequest("/v1/diff/1", HttpMethod.Get);
                await CheckResponse(client, request, HttpStatusCode.NotFound);

                request = MakeRequest("/v1/diff/1/left", HttpMethod.Put, "{\"data\":\"AAAAAA==\"}");
                await CheckResponse(client, request, HttpStatusCode.Created);

                request = MakeRequest("/v1/diff/1", HttpMethod.Get);
                await CheckResponse(client, request, HttpStatusCode.NotFound);

                request = MakeRequest("/v1/diff/1/right", HttpMethod.Put, "{\"data\":\"AAAAAA==\"}");
                await CheckResponse(client, request, HttpStatusCode.Created);
              
                request = MakeRequest("/v1/diff/1", HttpMethod.Get);
                await CheckResponse(client, request, HttpStatusCode.OK, JObject.Parse(@"{'diffResultType':'Equals'}"));

                request = MakeRequest("/v1/diff/1/right", HttpMethod.Put, "{\"data\":\"AQABAQ ==\"}");
                await CheckResponse(client, request, HttpStatusCode.Created);

                exp_cont = JObject.Parse(
                    @"{
                      'diffResultType': 'ContentDoNotMatch',
                      'diffs': [
                        {
                          'offset': 0,
                          'length': 1
                        },
                        {
                          'offset': 2,
                          'length': 2
                        }
                      ]
                    }"
                );
                request = MakeRequest("/v1/diff/1", HttpMethod.Get);
                await CheckResponse(client, request, HttpStatusCode.OK, exp_cont);

            }

        }
        public async Task GetWrongURI()
        {
            
        }

        private static HttpRequestMessage MakeRequest(string url, HttpMethod method, string content = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, url);
            if (content != null)
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");//CONTENT-TYPE header
            return request;
        }

        private static async Task CheckResponse(HttpClient client, HttpRequestMessage request, HttpStatusCode exp_status_code, JObject exp_content = null)
        {
            using (var response = await client.SendAsync(request))
            {
                Assert.AreEqual(exp_status_code, response.StatusCode);

                if (exp_content != null)
                {
                    var content = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    Assert.IsTrue(JToken.DeepEquals(content, exp_content));
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