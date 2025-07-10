using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Database
{
    public partial class FrpDataset
    {
        public string GetName()
        {
            if (string.IsNullOrEmpty(Name))
                return DatasetId;

            else return Name;
        }
    }
}
