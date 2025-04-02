using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DatabaseSchemaReader.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Patchwork.Api.Controllers;
using Patchwork.Authorization;
using Patchwork.Repository;
using Patchwork.SqlDialects;
using Patchwork.SqlDialects.Sqlite;

namespace Patchwork.Tests.Controllers
{

    public class ControllerTests
    {
        private ILogger<PatchworkController> log;
        private ISqlDialectBuilder sql;
        private IPatchworkAuthorization auth;
        private Mock<IHttpContextAccessor> accessor;
    private Mock<IPatchworkRepository> RepositoryMock;

        public ControllerTests()
        {
            var LogMock = new Mock<ILogger<PatchworkController>>();
            log = LogMock.Object;

            var SqlMock = new Mock<ISqlDialectBuilder>();
            sql = SqlMock.Object;

            accessor = new Mock<IHttpContextAccessor>();

            auth = new DefaultPatchworkAuthorization();

            RepositoryMock = new Mock<IPatchworkRepository>();
        }

        [Fact]
        public void Does_GetListEndpoint_SupplyDefaultLimitAndOffset()
        {
      //Arrange

      RepositoryMock
        .Setup(a => a.GetList(
          It.IsAny<string>(), //schemaName
          It.IsAny<string>(), //entityName
          It.IsAny<string>(), //fields
          It.IsAny<string>(), //filter
          It.IsAny<string>(), //sort
          It.IsAny<int>(), //limit
          It.IsAny<int>() //offset
        ))
        .Returns((
          string schemaName,
          string entityName,
          string fields,
          string filter,
          string sort,
          int limit,
          int offset
        ) =>
    new GetListResult(
        new List<dynamic>() { "item", "item", "item" }, 52, "LastId", limit, offset)
    );

      var MockHttpContext = new DefaultHttpContext();
            var request = MockHttpContext.Request;
            request.Headers["Range"] = "items=40-50";
            accessor.Setup(x => x.HttpContext).Returns(MockHttpContext);

            PatchworkController SUT = new PatchworkController(log, RepositoryMock.Object, auth, sql, accessor.Object);
            

            //Act
            var output = SUT.GetList("", "Does_GetListEndpoint_SupplyDefaultLimitAndOffset", "", "", "");

            //Assert
            Assert.NotNull(output);
            var json = output as JsonResult;
            Assert.NotNull(json);
            Assert.Equal(3, (json.Value as List<object>)?.Count);

            Assert.Equal("items 40-43/52", MockHttpContext.Response.Headers["Content-Range"]);
        }

        [Fact]
        public void Does_GetResourceEndpoint_ReturnNotFoundOnNoRecordsFound()
        {

      //Arrange
      RepositoryMock
    .Setup(a => a.GetResource(
        It.IsAny<string>(), //schemaName
        It.IsAny<string>(), //entityName
        It.IsAny<string>(), //id
        It.IsAny<string>(), //fields
        It.IsAny<string>() //include
        ))
    .Returns((
        string schemaName,
        string entityName,
        string id,
        string filter,
        string include
        ) =>
    new GetResourceResult(null)
    );

      var MockHttpContext = new DefaultHttpContext();
            accessor.Setup(x => x.HttpContext).Returns(MockHttpContext);

            PatchworkController SUT = new PatchworkController(log, RepositoryMock.Object, auth, sql, accessor.Object);

            //Act
            var output = SUT.GetResource("", "Does_GetResourceEndpoint_ReturnNotFoundOnNoRecordsFound", "");

            //Assert
            Assert.IsType<NotFoundResult>(output);
        }

    [Fact]
    public void Does_GetResourceEndpoint_ReturnHistoricalRecord()
    {
      //Arrange
      var MockHttpContext = new DefaultHttpContext();
      var request = MockHttpContext.Request;
      accessor.Setup(x => x.HttpContext).Returns(MockHttpContext);

      PatchworkController SUT = new PatchworkController(log, RepositoryMock.Object, auth, sql, accessor.Object);

      //Act
      var output = SUT.GetResource("", "", "", "", "", new DateTimeOffset(2025, 6, 5, 25, 11, 0, new TimeSpan(3, 0, 0)));
    }
  }
}
