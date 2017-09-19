using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HttpBatchHandler.Website.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new[] {"value1", "value2"};
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return id.ToString();
        }

        // GET api/values/query?id=5
        [HttpGet("query")]
        public string GetFromQuery([FromQuery] int id)
        {
            return id.ToString();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // GET api/values/File/1
        [HttpPost("File/{id}")]
        public async Task<IActionResult> UploadFile(int id)
        {
            if (HttpContext.Request.HasFormContentType)
            {
                var file = HttpContext.Request.Form.Files.Single();
                string b64Name;
                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(file.OpenReadStream());
                    b64Name = Convert.ToBase64String(hash);
                }
                if (b64Name == file.Name)
                {
                    return Ok();
                }
            }
            return BadRequest();
        }

        // GET api/values/File
        [HttpGet("File")]
        public async Task<IActionResult> GetFile()
        {
            var random = new Random();
            var buffer = new byte[1 << 16];
            string b64Name;
            var ms = new MemoryStream();
                    for (int i = 0; i < 10; i++)
                    {
                        random.NextBytes(buffer);
                        await ms.WriteAsync(buffer, 0, buffer.Length);
                    }

            ms.Position = 0;
            using (var md5 = MD5.Create())
            {
                md5.ComputeHash(ms);
                b64Name = Convert.ToBase64String(md5.Hash);
            }
            ms.Position = 0;
            return File(ms, "application/octet-stream", b64Name);
        }
    }
}