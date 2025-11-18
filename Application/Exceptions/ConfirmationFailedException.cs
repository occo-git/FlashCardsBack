using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Exceptions
{
    public class ConfirmationFailedException : Exception
    {
        public ConfirmationFailedException(string message) : base(message) { }
    }
}
