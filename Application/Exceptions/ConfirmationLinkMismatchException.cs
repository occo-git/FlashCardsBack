using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Exceptions
{
    public class ConfirmationLinkMismatchException : Exception
    {
        public ConfirmationLinkMismatchException(string message) : base(message) { }
    }
}
