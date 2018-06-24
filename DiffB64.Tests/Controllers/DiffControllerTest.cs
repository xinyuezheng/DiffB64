using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DiffB64.Controllers;
using System.Net;
using System.Web.Http.Controllers;

namespace DiffB64.Tests.Controllers
{

    [TestClass]
    public class DiffControllerTest
    {
        DiffController controller;

        //Populate data with Tuple(id, leftstring, rightstring)
        [TestInitialize]
        public void TestSetup()
        {
            controller = new DiffController();
            controller.Request = new HttpRequestMessage();
            controller.Configuration = new HttpConfiguration();
        }

        //session_id must be not null
        private void AddFakeDataToController(string session_id, int id, string left, string right)
        {
            DiffController.g_data.Clear();
            var b64data = new Dictionary<Tuple<int, string>, byte[]>();
            DiffController.g_data.Add(session_id, b64data);
            
            if (left != null)
            {
                byte[] binary = Convert.FromBase64String(left);
                b64data.Add(Tuple.Create(id, "left"), binary);
            }

            if (right != null)
            {
                byte[] binary = Convert.FromBase64String(right);
                b64data.Add(Tuple.Create(id, "right"), binary);
            }
        }

        [TestMethod, TestCategory("Get")]
        public void GetEmpty()
        {
            try
            {
                var response = controller.Get(1);
                Assert.Fail("Get should fail with empty data");
            }
            catch (HttpResponseException ex)
            {
                Assert.AreEqual(ex.Response.StatusCode, HttpStatusCode.NotFound);
            }
        }

        [TestMethod, TestCategory("Get")]
        public void GetHalfEmpty()
        {

            var left = new string[] { "AAAAAA==", null };
            var right = new string[] { null, "AAAAAA==" };
            var id = 1;
            var session_id = new string[] {"1234567", "1234abc" };

            for (int i = 0; i < left.Length; i++)
            {
                AddFakeDataToController(session_id[i], id, left[i], right[i]);
                try
                {
                    controller.Request.Headers.Clear();
                    controller.Request.Headers.Add("cookie", "session-id="+session_id[i]);

                    var response = controller.Get(id);
                    Assert.Fail("Get should fail with data half missing");
                }
                catch (HttpResponseException ex)
                {
                    Assert.AreEqual(ex.Response.StatusCode, HttpStatusCode.NotFound);
                }
            }
        }

        [TestMethod, TestCategory("Get")]
        public void GetSizeNotEqual()
        {
            var session_id = "4567atre";
            int id = 2;
            AddFakeDataToController(session_id, id, "AAAAAA==", "AAA=");

            controller.Request.Headers.Add("cookie", "session-id="+session_id);
            var response = controller.Get(id);

            Assert.AreEqual(response.diffResultType, "SizeDoNotMatch");
            Assert.AreEqual(response.ShouldSerializediffs(), false);
        }


        [TestMethod, TestCategory("Get")]
        public void GetEqual()
        {
            var session_id = "4567atre";
            int id = 2;
            AddFakeDataToController(session_id, id, "AAAAAA==", "AAAAAA==");

            controller.Request.Headers.Add("cookie", "session-id=" + session_id);
            var response = controller.Get(id);

            Assert.AreEqual(response.diffResultType, "Equals");
            Assert.AreEqual(response.ShouldSerializediffs(), false);
        }

        [TestMethod, TestCategory("Get")]
        public void GetNoEqual()
        {
            var session_id = "4567atre";
            int id = 2;
            AddFakeDataToController(session_id, id, "AAA=", "AAB=");
            //var putdata = new DiffController.PutData { data = "AAA=" };
            //var putdata = new DiffController.PutData { data = "AAB=" };


            controller.Request.Headers.Add("cookie", "session-id=" + session_id);
            var response = controller.Get(id);

            Assert.AreEqual(response.diffResultType, "SizeDoNotMatch");
            Assert.AreEqual(response.ShouldSerializediffs(), false);
        }


        [TestMethod, TestCategory("Get")]
        public void GetNotEqual()
        {
            var session_id = "4567atre";
            int id = 2;
            AddFakeDataToController(session_id,id, "AAAAAA==", "AQABAQ==");

            controller.Request.Headers.Add("cookie", "session-id=" + session_id);
            var response = controller.Get(id);

            Assert.AreEqual(response.diffResultType, "ContentDoNotMatch");
            Assert.AreEqual(response.ShouldSerializediffs(), true);
            Assert.AreEqual(response.diffs[0]["offset"], 0);
            Assert.AreEqual(response.diffs[0]["length"], 1);
            Assert.AreEqual(response.diffs[1]["offset"], 2);
            Assert.AreEqual(response.diffs[1]["length"], 2);
        }

        [TestMethod, TestCategory("Put")]
        public void Put()
        {
            var session_id = "ffffessl2";
            int[] id = { 1, 4, 0x7fffffff };
            string[] pos = { "left", "right" };
            var putdata = new DiffController.PutData { data = "AAAAAA==" };

            controller.Request.Headers.Add("cookie", "session-id=" + session_id);
            AddFakeDataToController(session_id, 0, null, null);

            //PUT /v1/diff/left/1 with "AAAAAA=="
            for (int i = 0; i < id.Length; i++)
            {
                for (int j = 0; j < pos.Length; j++)
                {
                    var response = controller.Put(id[i], pos[j], putdata);
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.Created);

                    //Verify data is stored in b64data
                    var key_left = new Tuple<int, string>(id[i], pos[j]);
                    Assert.IsTrue(DiffController.g_data[session_id][key_left].SequenceEqual(Convert.FromBase64String(putdata.data)));
                }
            }
        }

        [TestMethod, TestCategory("Put")]
        public void PutNonB64()
        {
            var session_id = "ffffessl2";
            int id = 1;
            string pos = "left";
            var putdata = new DiffController.PutData { data = "AXAA==" };

            controller.Request.Headers.Add("cookie", "session-id=" + session_id);
            AddFakeDataToController(session_id, 0, null, null);
            try
            {
                var response = controller.Put(id, pos, putdata);
                Assert.Fail("Put NonB64 data is not allowed");
            }
            catch (HttpResponseException ex)
            {
                Assert.AreEqual(ex.Response.StatusCode, (HttpStatusCode)422);
            }
        }

        [TestMethod, TestCategory("Put")]
        public void PutDataNull()
        {
            var session_id = "ffffessl2";
            int id = 1;
            string pos = "left";
            var putdata = new DiffController.PutData { data = null };

            AddFakeDataToController(session_id, 0, null, null);
            controller.Request.Headers.Add("cookie", "session-id=" + session_id);
            try
            {
                var response = controller.Put(id, pos, putdata);
                Assert.Fail("Put Null is not allowed");
            }
            catch (HttpResponseException ex)
            {
                Assert.AreEqual(ex.Response.StatusCode, HttpStatusCode.BadRequest);
            }
        }

        [TestMethod, TestCategory("Put")]
        public void PutNull()
        {
            var session_id = "ffffessl2";
            int id = 1;
            string pos = "left";

            AddFakeDataToController(session_id, 0, null, null);
            controller.Request.Headers.Add("cookie", "session-id=" + session_id);
            try
            {
                var response = controller.Put(id, pos, null);
                Assert.Fail("Put Null is not allowed");
            }
            catch (HttpResponseException ex)
            {
                Assert.AreEqual(ex.Response.StatusCode, HttpStatusCode.NoContent);
            }
        }

    }
}
