using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using _3d_photo_api.Adapter;
using Audit.WebApi;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using photo_api.Helpers;
using photo_api.Shell;

namespace photo_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [RequestFormLimits(ValueCountLimit = 5000)]
    [EnableCors]
    [AuditApi]
    public class PhotoController : ControllerBase
    {
        private static long Max_Upload_Size = long.Parse(Startup.Configuration["AppSettings:MaxUploadSize"]);
        private static string Root_Folder = Startup.Configuration["AppSettings:RootFolder"];
        private readonly ILogger<PhotoController> _logger;
        private readonly PhotoAdapter _photoAdapter = new PhotoAdapter();

        public PhotoController(ILogger<PhotoController> logger)
        {
            _logger = logger;
        }

        [HttpPost("p")]
        [Produces("application/json")]
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
        [RequestSizeLimit(209715200)]
        public async Task<ActionResult<PhotoProcessResult>> Process()
        {
            if (Request.Form.Files?.Count == 0)
            {
                return BadRequest("No images to process");
            }

            var totalBytes = Request.Form.Files.Sum(f => f.Length);
            if (totalBytes > Max_Upload_Size)
            {
                return BadRequest($"Can't process more than {Max_Upload_Size / 1024:N0} Mb of data");
            }

            var traceId = this.HttpContext.TraceIdentifier.Split(':')[0];
            var result = await Process(traceId);

            return Ok(result);
        }

        [HttpGet("d")]
        public ActionResult Download([FromQuery(Name = "t")] string traceId)
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return BadRequest();
            }
            if (traceId != ShellHelper.SanitizeFilename(traceId))
            {
                return BadRequest("Don't try to cheat me");
            }
            var file = $"{Root_Folder}/{traceId}/output/videos.zip";
            if (System.IO.File.Exists(file))
            {
                return PhysicalFile(file, "application/zip", file);
            }
            return Problem($"File {file} not found");
        }

        private async Task<PhotoProcessResult> Process(string traceId)
        {
            // Copy images to input folder
            var inputImagesFolder = $"{Root_Folder}/{traceId}/images";
            Directory.CreateDirectory(inputImagesFolder);
            foreach (var file in Request.Form.Files)
            {
                var fileName = ShellHelper.SanitizeFilename(file.FileName);
                if (string.IsNullOrEmpty(Path.GetExtension(file.FileName)))
                {
                    // Assume is a jpg
                    fileName += ".jpg";
                }
                var filePath = $"{inputImagesFolder}/{fileName}";
                if (!System.IO.File.Exists(filePath))
                {
                    using (var output = System.IO.File.Create(filePath))
                    {
                        await file.CopyToAsync(output);
                    }
                }
            }

            // Create config
            var outputVideoFolder = $"{Root_Folder}/{traceId}/video";
            var settings = new Dictionary<string, object>()
            {
                { "src_folder", inputImagesFolder },
                { "video_folder", outputVideoFolder }
                //{ "offscreen_rendering", "True" }
            };
            var configFolder = $"{Root_Folder}/{traceId}/config";
            var configFile = $"{configFolder}/argument.yml";
            ConfigHelper.WriteConfig(configFile, settings);

            // Execute
            var result = _photoAdapter.Execute(inputImagesFolder, configFile);

            if (result.ErrorCount > 0)
            {
                throw new Exception(string.Join(' ', result.Errors));
            }

            // Make zip
            var outputZip = $"{Root_Folder}/{traceId}/output/videos.zip";
            MakeZip(outputVideoFolder, outputZip);

            // Delete temp folders
            Directory.Delete(inputImagesFolder, true);
            Directory.Delete(configFolder, true);

            result.TraceId = traceId;

            return result;
        }

        private void MakeZip(string inputFolder, string outputFilePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            using (var zip = new FileStream(outputFilePath, FileMode.Create))
            {
                using (var archive = new ZipArchive(zip, ZipArchiveMode.Create))
                {
                    foreach(var file in Directory.GetFiles(inputFolder))
                    {
                        archive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                }
            }
        }

    }
}
