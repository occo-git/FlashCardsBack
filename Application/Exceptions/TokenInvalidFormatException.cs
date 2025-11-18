using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Exceptions
{
    public class TokenInvalidFormatException : Exception
    {
        public TokenInvalidFormatException(string message) : base(message) { }
    }
}
