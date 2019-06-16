using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace HttpBatchHandler.Benchmarks.Tools
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
        public IEnumerable<string> Get() => new[] {"value1", "value2"};

        // GET api/values/5
        [HttpGet("{id}", Name = "GetById")]
        public string Get(int id) => id.ToString();

        // GET api/values/query?id=5
        [HttpGet("query")]
        public string GetFromQuery([FromQuery] int id) => id.ToString();
    }
}