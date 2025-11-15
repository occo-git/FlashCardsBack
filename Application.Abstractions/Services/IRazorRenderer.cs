using Application.DTO;
using Application.DTO.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IRazorRenderer
    {
        Task<string> RenderViewToStringAsync<TModel>(RenderTemplates renderTemplate, TModel model);
    }
}
