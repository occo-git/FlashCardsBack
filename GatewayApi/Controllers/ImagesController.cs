using Infrastructure.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace GatewayApi.Controllers
{
    [Route("api/images")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;

        public ImagesController(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        //[HttpGet("{fileId}")]
        //public async Task<IActionResult> GetFile(string fileId)
        //{
        //    using var stream = await _fileStorageService.GetFileStreamAsync(fileId);
        //    {
        //        if (stream == null)
        //            return NotFound();
                
        //        return File(stream, "application/octet-stream", fileId);
        //    }
        //}

        /// <summary>
        /// GET: api/images/{fileId}
        /// </summary>
        /// <param name="fileId"></param>
        /// <returns></returns>
        [HttpGet("{fileId}")]
        public IActionResult GetFile(string fileId)
        {
            try
            {
                var filePath = _fileStorageService.GetFilePathAsync(fileId);
                var contentType = "application/octet-stream";
                return PhysicalFile(filePath, contentType, fileId, enableRangeProcessing: true);
            }
            catch (FileNotFoundException)
            {
                return NotFound("File not found");
            }
        }
    }
}
