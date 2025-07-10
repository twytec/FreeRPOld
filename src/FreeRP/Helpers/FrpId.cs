using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Helpers
{
    public static class FrpId
    {
        public static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static string NewSmallId()
        {
            return Path.GetRandomFileName();
        }
    }
}
