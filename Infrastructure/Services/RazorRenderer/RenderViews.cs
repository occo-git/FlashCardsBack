using Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.RazorRenderer
{
    public static class RenderViews
    {
        public static Dictionary<RenderTemplates, string> Paths = new()
        {
            { RenderTemplates.ConfirmEmail, "~/Views/ConfirmEmail.cshtml" },
            { RenderTemplates.Greeting, "~/Views/Greeting.cshtml" }
        };
    }
}