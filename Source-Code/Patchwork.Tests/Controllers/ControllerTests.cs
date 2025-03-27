using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
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
        private IPatchworkRepository repo;

        public ControllerTests()
        {
            var LogMock = new Mock<ILogger<PatchworkController>>();
            log = LogMock.Object;

            var SqlMock = new Mock<ISqlDialectBuilder>();
            sql = SqlMock.Object;

            auth = new DefaultPatchworkAuthorization();

            var RepositoryMock = new Mock<IPatchworkRepository>();
            RepositoryMock
                .Setup(a => a.GetList(
                    It.IsAny<string>(), //schemaName
                    It.Is<string>(b => b == "DefaultLimitOffsetTest"), //entityName
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
                    new List<dynamic>(), 42, "LastId", limit, offset)
                );

            repo = RepositoryMock.Object;
        }

        [Fact]
        public void Does_GetListEndpoint_SupplyDefaultLimitAndOffset()
        {
            //Arrange
            PatchworkController SUT = new PatchworkController(log, repo, auth, sql);
            SUT.ControllerContext = GetMockContext();
            

            //Act
            var output = SUT.GetList("", "DefaultLimitOffsetTest", "", "", "");

            //Assert
            Assert.NotNull(output);
        }



        public static ControllerContext GetMockContext(PatchworkController controller) {
            var request = new Mock<HttpRequest>();
            request.SetupGet(x => x.Headers["X-Requested-With"]).Returns("XMLHttpRequest");

            var response = new Mock<HttpResponse>();
            response.Setup(r => r.Headers).Returns(new HeaderDictionary());

            var context = new Mock<HttpContext>();
            context.Setup(x => x.Request).Returns(request.Object);
            context.Setup(x => x.Response).Returns(response.Object);

            var actionContext = new Mock<ActionContext>();
            actionContext.Setup(x => x.HttpContext).Returns(context.Object);
            actionContext.Setup(x => x.RouteData).Returns();
            actionContext.Setup(x => x.ActionDescriptor).Returns();

            return new ControllerContext(actionContext.Object);
        }
    }
}
