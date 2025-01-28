using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Patchwork.Controllers
{
    [Route("{schema?}/{table?}")]
    [ApiController]
    public class EntityController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> Get(string schema, string table)
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet("{id}")]
        public string Get(int id, string schema, string table)
        {
            return $"value: {id}, {schema}, {table}";
        }

        [HttpPost]
        public void Post(string schema, string table, [FromBody] string value)
        {
        }

        [HttpPut("{id}")]
        public void Put(int id, string schema, string table, [FromBody] string value)
        {
        }

        [HttpDelete("{id}")]
        public void Delete(int id, string schema, string table)
        {
        }
    }
}
