using FileStorage.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace FileStorage.Controllers
{
    /// <summary>
    /// This sample controller reads the contents of an HTML file upload asynchronously and writes one or more body parts to a local file.
    /// </summary>
    [RoutePrefix("api/test")]
    public class FileUploadController : ApiController
    {
        static readonly string ServerUploadFolder = Path.GetTempPath();

        [Route("files")]
        [HttpPost]
        public async Task<FileResult> UploadFile()
        {
            string  contractId = "";
            string  clientId = "";

            // Verify that this is an HTML Form file upload request
            if (!Request.Content.IsMimeMultipartContent("form-data"))
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.UnsupportedMediaType));
            }

            var catalog = Properties.Settings.Default["FileStorageCatalog"].ToString();
            // Create a stream provider for setting up output streams
            MultipartFormDataStreamProvider streamProvider = new MultipartFormDataStreamProvider(ServerUploadFolder);

            // Read the MIME multipart asynchronously content using the stream provider we just created.
            await Request.Content.ReadAsMultipartAsync(streamProvider);
            var data = streamProvider.FormData;
            foreach (var item in streamProvider.FormData.Keys)
            {
                if (item.ToString() == "ContractId")
                    contractId = streamProvider.FormData.GetValues(item.ToString()).First();
                if (item.ToString() == "ClientId")
                    clientId = data.GetValues(item.ToString()).First();
            }
            var path = catalog;
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                path += $@"\client{clientId}";
            }
            if (!string.IsNullOrWhiteSpace(contractId))
            {
                path += $@"\contract{contractId}";
            }
            if (!Directory.Exists(path))
            {
                try
                {
                    DirectoryInfo di = Directory.CreateDirectory(path);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            var fileNames = new List<string>();
            for (int i = 0; i < streamProvider.FileData.Count; i++)
            {
                var file = streamProvider.FileData[i];
                var fileName = file.Headers.ContentDisposition.FileName.Replace("\"", "");
                File.Move($"{file.LocalFileName}", 
                    $@"{catalog}\{fileName}");
                fileNames.Add(fileName);
            }

            var name = streamProvider.FileData[0].Headers.ContentDisposition.FileName;

            // Create response
            return new FileResult
            {
                FileNames = fileNames,
                Submitter = streamProvider.FormData["submitter"]
            };
        }
    }
}