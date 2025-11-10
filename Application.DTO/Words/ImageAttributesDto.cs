using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Words
{
    public record ImageAttributesDto(string By, string Link, string Source)
    {
        public static ImageAttributesDto Empty => new ImageAttributesDto(string.Empty, string.Empty, string.Empty);
    }
}
