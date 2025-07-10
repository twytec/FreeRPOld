using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.User
{
    public partial class FrpUser
    {
        public string GetName()
        {
            if (string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName))
                return Email;

            return $"{FirstName} {LastName}".Trim();
        }
    }
}
