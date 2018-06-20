using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Script.Serialization;

namespace DiffB64.Controllers
{
    [RoutePrefix("v1/diff")]
    public class DiffController : ApiController
    {
        static public Dictionary<Tuple<int, string>, byte[]> b64data = new Dictionary<Tuple<int, string>, byte[]>();

        // GET v1/diff/{id}
        [Route("{id:int:min(1)}")]
        public DiffResults Get(int id)
        {         
            var key_left = new Tuple<int, string>(id, "left");
            var key_right = new Tuple<int, string>(id, "right");
            byte[] binary_left, binary_right;
            try
            {
                binary_left = b64data[key_left];
                binary_right = b64data[key_right];
            }
            catch (KeyNotFoundException)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            }

            if (binary_left.Length != binary_right.Length)
            {
                return DiffResSizeNotEqual();
            }

            return DiffResSizeEqual(binary_left, binary_right);
        }

        private static DiffResults DiffResSizeNotEqual()
        {
            var diff_result = new DiffResults();
            diff_result.diffResultType = "SizeDoNotMatch";

            return diff_result;
        }

        public class DiffResults
        {
            public string diffResultType;
            public List<Dictionary<string, int>> diffs;

            public DiffResults()
            {
                diffs = new List<Dictionary<string, int>>();
            }

            public bool ShouldSerializediffs()
            {
                return (diffs.Count > 0);
            }
        }

        private DiffResults DiffResSizeEqual(byte[] binary_left, byte[] binary_right)
        {
            var diff_results = new DiffResults();
            int diff_length = 0, offset;

            for (offset = 0; offset < binary_left.Length; offset++)
            {
                if (binary_left[offset] == binary_right[offset])
                {
                    AddToDiffRes(diff_results, offset, diff_length);
                    diff_length = 0;
                }
                else
                {
                    diff_length++;
                }
            }

            AddToDiffRes(diff_results, offset, diff_length);

            if (diff_results.diffs.Count != 0)
                diff_results.diffResultType = "ContentDoNotMatch";
            else
                diff_results.diffResultType = "Equals";

            return diff_results;
        }

        private void AddToDiffRes(DiffResults diff_results, int offset, int diff_length)
        {
            if (diff_length == 0)
                return;
            
            var diff = new Dictionary<string, int>()
                    {
                        { "offset", offset-diff_length }, //Look back to the start of the diff
                        { "length", diff_length }
                    };
            diff_results.diffs.Add(diff);            
        }

       

        public class PutData {
            public string data;
        }

        // PUT v1/diff/{id}/{pos}  1<= id <= 2,147,483,647 (or 0x7FFF,FFFF) is the maximum positive value for a 32-bit
        [Route("{id:int:min(1)}/{pos:alpha:regex([left|right])}")]
        public HttpResponseMessage Put(int id, string pos, [FromBody]PutData put_data)
        {
            byte[] binary;
            try
            {
                binary = Convert.FromBase64String(put_data.data);
            }
            catch (FormatException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse((HttpStatusCode)422, "Must pass base64 format"));
            }
            catch (ArgumentNullException)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Put null value is not allowed"));
            }

            var key = Tuple.Create(id, pos);
            b64data[key] = binary;

            return Request.CreateResponse(HttpStatusCode.Created, "Create OK");
        }
    }
}
