using System.Security.Principal;

namespace Patchwork.Authorization;

public class DefaultPatchworkAuthorization : IPatchworkAuthorization
{
  public Permission GetPermission(string path, IPrincipal principal)
  {
    return GetRoleAccess(principal);
  }
  public Permission GetPermissionToCollection(string schema, string entity, IPrincipal principal)
  {
    return GetRoleAccess(principal);
  }
  public Permission GetPermissionToResource(string schema, string entity, string id, IPrincipal principal)
  {
    return GetRoleAccess(principal);
  }

  private static Permission GetRoleAccess(IPrincipal principal)
  {
    var access = Permission.None;
    if (principal.IsInRole(Permission.Viewer.ToString()))
      access |= Permission.Viewer;
    if (principal.IsInRole(Permission.Editor.ToString()))
      access |= Permission.Editor;
    if (principal.IsInRole(Permission.Owner.ToString()))
      access |= Permission.Owner;
    if (principal.IsInRole(Permission.Manager.ToString()))
      access |= Permission.Manager;
    if (principal.IsInRole(Permission.Admin.ToString()))
      access |= Permission.Admin;

    return access;
  }
}
