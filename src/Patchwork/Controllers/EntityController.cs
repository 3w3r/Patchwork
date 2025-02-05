using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Patchwork.SqlDialects;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Patchwork.Controllers
{
    [Route("{schema?}/{table?}")]
    [ApiController]
    public class EntityController : ControllerBase
    {
        private readonly ISqlDialectBuilder _dialectBuilder;

        public EntityController(ISqlDialectBuilder dialectBuilder)
        {
            _dialectBuilder = dialectBuilder;
        }

        [HttpGet]
        public IEnumerable<string> Get(string schema, string table, [FromQuery] string? fields, [FromQuery] string? filter, [FromQuery] string? sort, [FromQuery] int limit, [FromQuery] int offset)
        {
            try
            {
                var thing = _dialectBuilder.BuildGetListSql(schema, table, fields, filter, sort, limit, offset);

                return new string[] { thing.ToString() };
            }
            catch (Exception ex) {
                return new string[] { "Error" };
            }
        }

        //Get-Record
        [HttpGet("{id}")]
        public string Get(int id, string schema, string table, [FromQuery] string? fields, [FromQuery] string[]? include)
        {
            return $"value: {id}, {schema}, {table}";
        }

        //Post-Record
        [HttpPost]
        public void Post(string schema, string table, [FromBody] string value)
        {
        }

        //Put-Record
        [HttpPut("{id}")]
        public void Put(int id, string schema, string table, [FromBody] string value)
        {
        }

        //Delete-Record
        [HttpDelete("{id}")]
        public void Delete(int id, string schema, string table)
        {
        }
    }
}
