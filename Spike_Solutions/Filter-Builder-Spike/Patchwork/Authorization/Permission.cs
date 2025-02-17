namespace Patchwork.Authorization;

[Flags]
public enum Permission
{
  // Actions a user can perform
  None = 0x00000000, // no access at all
  Get = 0x00000001, // can read single resource
  Search = 0x00000010, // can query listing with filter
  Post = 0x00000100, // can create a new entry in this listing
  Put = 0x00001000, // can edit a single resource
  Delete = 0x00010000, // can remove a single resource
  Patch = 0x00100000, // can use JSON Patch to update a resource
  Bulk = 0x01000000, // can use JSON Patch for bulk updates to a list
  Options = 0x10000000, // can change user permissions to a resource or list

  // Role-Based Access Control (RBAC) expressed as a combination of Permission values
  Viewer = Get | Search,
  Editor = Viewer | Post | Put | Delete | Patch,
  Owner = Editor | Options,
  Manager = Editor | Bulk,
  Admin = Get | Search | Post | Put | Delete | Patch | Bulk | Options,
}
