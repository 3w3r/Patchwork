using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatabaseSchemaReader.Filters;
using Moq;
using Patchwork.Repository;

namespace Patchwork.Tests.Controllers
{

    public class ControllerTests
    {
        ControllerTests()
        {
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
        }

        [Fact]
        public void Does_GetListEndpoint_SupplyDefaultLimitAndOffset()
        {
            //Arrange

            //Act
            //Assert
        }

    }
}
