using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patchwork;

public static class PatchworkConfig
{
  public static int MinRecordLimit { get; set; } = 25;
  public static int MaxRecordLimit { get; set; } = 5000;
}
