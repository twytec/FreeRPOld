using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP
{
    public class FrpException(FrpErrorType errorType, string message) : Exception
    {
        public FrpErrorType ErrorType { get; set; } = errorType;
        public override string Message => message;

        public static void Error(FrpErrorType errorType, string message)
        {
            throw new FrpException(errorType, message);
        }

        public static FrpException GetFrpException(FrpErrorType errorType, string message)
        {
            return new FrpException(errorType, message);
        }
    }
}
