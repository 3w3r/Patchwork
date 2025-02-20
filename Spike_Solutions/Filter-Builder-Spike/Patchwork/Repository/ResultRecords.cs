using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Json.Patch;

namespace Patchwork.Repository;

public record GetListResult(List<dynamic> Resources, long TotalCount, string LastId, int Limit, int Offset);
public record GetResourceResult(dynamic Resource);
public record PostResult(string id, dynamic Resource, JsonPatch Changes);
public record PutResult(dynamic Resource, JsonPatch Changes);
public record DeleteResult(bool Success, string Id);
public record PatchResourceResult(string id, dynamic Resource, JsonPatch Changes);
public record PatchListResult(List<PatchResourceResult> Inserted, List<PatchResourceResult> Updated, List<PatchResourceResult> Deleted);
