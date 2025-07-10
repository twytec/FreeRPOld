using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP
{
    public partial class FrpPermission
    {
        public void SetMemberIdAccessUri()
        {
            MemberIdAccessUri = MemberId + AccessUri;
        }
    }
}
