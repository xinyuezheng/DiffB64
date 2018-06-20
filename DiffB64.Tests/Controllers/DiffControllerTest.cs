using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DiffB64.Controllers;
using System.Net;
using static DiffB64.Controllers.DiffController;
using System.Web.Http.Routing;
using System.Web.Http.Hosting;
using System.Web.Http.Controllers;

namespace DiffB64.Tests.Controllers
{
    [TestClass]
    public class DiffControllerTest
    {
        [TestInitialize]
        public void InitB64()
        {
            b64data.Clear();
        }

        //Populate data with Tuple(id, leftstring, rightstring)
        private DiffController CreateController(int id, string left, string right)
        {
            var controller = new DiffController();
            controller.Request = new HttpRequestMessage();
            controller.Configuration = new HttpConfiguration();

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

            return controller;
        }

        [TestMethod, TestCategory("Get")]
        [ExpectedException(typeof(HttpResponseException))]
        public void GetEmpty()
        {            
            var controller = CreateController(0, null, null);

            //GET /v1/diff/1           
            var response = controller.Get(1);
        }

        [TestMethod, TestCategory("Get")]
        [ExpectedException(typeof(HttpResponseException))]
        public void GetHalfEmpty()
        {
            var left = new string[]{ "AAAAAA==", null };
            var right = new string[]{ null, "AAAAAA==" };
            var id = 1;

            for (int i = 0; i < left.Length; i++)
            {
                var controller = CreateController(id, left[i], right[i]);

                var response = controller.Get(id);
            }
        }

        [TestMethod, TestCategory("Get")]
        public void GetSizeNotEqual()
        {
            int id = 2;
            var controller = CreateController(id, "AAAAAA==", "AAA=");

            var response = controller.Get(id);

            //Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);       
            Assert.AreEqual(response.diffResultType, "SizeDoNotMatch");
            Assert.AreEqual(response.diffs.Count, 0);
        }


        [TestMethod, TestCategory("Get")]
        public void GetEqual()
        {
            int id = 2;            
            var controller = CreateController(id, "AAAAAA==", "AAAAAA==");

            var response = controller.Get(id);

 //           Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);       
            Assert.AreEqual(response.diffResultType, "Equals");
        }

        [TestMethod, TestCategory("Get")]
        public void GetNotEqual()
        {
            int id = 2;
            var controller = CreateController(id, "AAAAAA==", "AQABAQ==");

            var response = controller.Get(id);

            //           Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);       
            Assert.AreEqual(response.diffResultType, "ContentDoNotMatch");
            Assert.AreEqual(response.diffs[0]["offset"], 0);
            Assert.AreEqual(response.diffs[0]["length"], 1);
            Assert.AreEqual(response.diffs[1]["offset"], 2);
            Assert.AreEqual(response.diffs[1]["length"], 2);
        }

        [TestMethod, TestCategory("Put")]
        public void Put()
        {
            int[] id = { 1, 4, 0x7fffffff};
            string[] pos = { "left", "right" };
            var putdata = new PutData { data = "AAAAAA == "};

            var controller = CreateController(0, null, null);

            //PUT /v1/diff/left/1 with "AAAAAA=="
            for (int i = 0; i < id.Length; i++)
            {
                for(int j = 0; j < pos.Length; j++)
                {
                    var response = controller.Put(id[i], pos[j], putdata);
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.Created);

                    //Verify data is stored in b64data
                    var key_left = new Tuple<int, string>(id[i], pos[j]);
                    Assert.IsTrue(b64data[key_left].SequenceEqual(Convert.FromBase64String(putdata.data)));
                }
            }

        }

    }
}
