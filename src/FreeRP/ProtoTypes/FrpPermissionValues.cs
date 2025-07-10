using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP
{
    public partial class FrpPermissionValues
    {
        public static void SetAllValues(FrpPermissionValues a, FrpPermissionValue b) => a.Change = a.Add = a.Delete = a.Read = b;
        public static bool AreAll(FrpPermissionValues a, FrpPermissionValue b) => (a.Change == b && a.Add == b && a.Delete == b && a.Read == b);
        public static bool IsAny(FrpPermissionValues a, FrpPermissionValue b) => (a.Change == b || a.Add == b || a.Delete == b || a.Read == b);
    }
}
