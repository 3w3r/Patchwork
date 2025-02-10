using System.Security;
using System.Security.Principal;

namespace Patchwork.Authorization;

public interface IPatchworkAuthorization
{
  Permission GetPermission(string path, IPrincipal principal);
  Permission GetPermissionToCollection(string schema, string entity, IPrincipal principal);
  Permission GetPermissionToResource(string schema, string entity, string id, IPrincipal principal);
}
