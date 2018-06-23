using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web.Http;
using System.Net;
using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace DiffB64.Tests.Components
{
    [TestClass, TestCategory("Components")]
    public class DiffB64ComponentsTest
    {
        HttpConfiguration config;
        HttpServer server;
        HttpClient client;
        CookieBox cb;

        [TestInitialize]
        public void Setup()
        {
            config = new HttpConfiguration();
            WebApiConfig.Register(config);

            server = new HttpServer(config);
            client = MakeClient(server);
            cb = new CookieBox();
        }

        [TestMethod, TestCategory("HappyFlow")]
        public async Task DiffB64HappyFlow()
        {
            HttpRequestMessage request;
            JObject exp_cont;

            request = MakeRequest(cb.cookie, "/v1/diff/1", HttpMethod.Get);
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);

            request = MakeRequest(cb.cookie, "/v1/diff/1/left", HttpMethod.Put, "{\"data\":\"AAAAAA==\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.Created);

            request = MakeRequest(cb.cookie, "/v1/diff/1", HttpMethod.Get);
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);

            request = MakeRequest(cb.cookie, "/v1/diff/1/right", HttpMethod.Put, "{\"data\":\"AAAAAA==\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.Created);
              
            request = MakeRequest(cb.cookie, "/v1/diff/1", HttpMethod.Get);
            await CheckResponse(cb, client, request, HttpStatusCode.OK, JObject.Parse(@"{'diffResultType':'Equals'}"));

            request = MakeRequest(cb.cookie, "/v1/diff/1/right", HttpMethod.Put, "{\"data\":\"AQABAQ==\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.Created);

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
            request = MakeRequest(cb.cookie, "/v1/diff/1", HttpMethod.Get);
            await CheckResponse(cb, client, request, HttpStatusCode.OK, exp_cont);

            request = MakeRequest(cb.cookie, "/v1/diff/1/left", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.Created);

            request = MakeRequest(cb.cookie, "/v1/diff/1", HttpMethod.Get);
            await CheckResponse(cb, client, request, HttpStatusCode.OK, JObject.Parse(@"{'diffResultType':'SizeDoNotMatch'}"));

        }

        [TestMethod, TestCategory("Routing")]
        public async Task PutRouting()
        {
            HttpRequestMessage request;

            request = MakeRequest(cb.cookie, "/v1/diff/1", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.MethodNotAllowed);

            request = MakeRequest(cb.cookie, "/v1/diff", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);

            request = MakeRequest(cb.cookie, "/v1/diff/1/right/bla", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);

            request = MakeRequest(cb.cookie, "/v1/diff/-1.5/left", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);

            request = MakeRequest(cb.cookie, "/v1/diff/-1/left", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);

            request = MakeRequest(cb.cookie, "/v1/diff/bla/left", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);

            request = MakeRequest(cb.cookie, "/v1/diff/1/bla", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);
        }

        [TestMethod, TestCategory("Routing")]
        public async Task GetRouting()
        {
            HttpRequestMessage request;

            request = MakeRequest(cb.cookie, "/v1/diff/", HttpMethod.Get);
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);

            request = MakeRequest(cb.cookie, "/v1/diff/1/bla", HttpMethod.Get);
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);

            request = MakeRequest(cb.cookie, "/v1/diff/1/left", HttpMethod.Get);
            await CheckResponse(cb, client, request, HttpStatusCode.MethodNotAllowed);
        }

        [TestMethod, TestCategory("Routing")]
        public async Task BoundaryCheck()
        {
            HttpRequestMessage request;

            request = MakeRequest(cb.cookie, "/v1/diff/2147483647/left", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.Created);

            request = MakeRequest(cb.cookie, "/v1/diff/2147483647/right", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.Created);

            request = MakeRequest(cb.cookie, "/v1/diff/2147483647", HttpMethod.Get);
            await CheckResponse(cb, client, request, HttpStatusCode.OK);

            request = MakeRequest(cb.cookie, "/v1/diff/1/left", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.Created);

            request = MakeRequest(cb.cookie, "/v1/diff/1/right", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.Created);

            request = MakeRequest(cb.cookie, "/v1/diff/1", HttpMethod.Get);
            await CheckResponse(cb, client, request, HttpStatusCode.OK);

            request = MakeRequest(cb.cookie, "/v1/diff/2147483648/left", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);

            request = MakeRequest(cb.cookie, "/v1/diff/0/left", HttpMethod.Put, "{\"data\":\"AAA=\"}");
            await CheckResponse(cb, client, request, HttpStatusCode.NotFound);
        }

        private static HttpRequestMessage MakeRequest(string cookie, string url, HttpMethod method, string content = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, url);
            if (content != null)
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");//CONTENT-TYPE header

            if(cookie != null)
                request.Headers.Add("Cookie", cookie);

            return request;
        }

        private class CookieBox
        {
            public string cookie;
        }

        private static async Task CheckResponse(CookieBox container, HttpClient client, HttpRequestMessage request, HttpStatusCode exp_status_code, JObject exp_content = null)
        {
            using (var response = await client.SendAsync(request))
            {
                Assert.AreEqual(exp_status_code, response.StatusCode);

                if (exp_content != null)
                {
                    var content = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                    Assert.IsTrue(JToken.DeepEquals(content, exp_content));
                }

                IEnumerable<String> values;
                if (response.Headers.TryGetValues("Set-Cookie", out values))
                {
                    container.cookie = values.First();
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