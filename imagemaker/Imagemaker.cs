using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;

namespace imagemaker
{
    public static class Imagemaker
    {
        [FunctionName("Function1")]
        public static void Run([BlobTrigger("vertragseingang/{name}", Connection = "")]Stream myBlob, string name, TraceWriter log)
        {
            // Informationen über den verarbeiteten Blob ins Log schreiben
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            // Zielkontainer bestimmen
            var cloudStorageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=visionsto;AccountKey=+2UjbuojMOTH6V1i6Mr3eqv4XxogNSYHJMbMVQMN8vL01It73xyPiEzUeZvF0NAe0VU2lgmDp/nj/CN0ss5u+A==;EndpointSuffix=core.windows.net");
            log.Info($"Get CloudBlobClient");
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            var targetContainer = blobClient.GetContainerReference("bildeingang");

            // Dateigröße für das entstehende JPG festlegen
            int desired_x_dpi = 300;
            int desired_y_dpi = 300;

            // Variable mit der Versionsinfo der GhostScript DLL anlegen
            // GhostscriptVersionInfo gvi = new GhostscriptVersionInfo(@"C:\Users\UweZabel\source\repos\imagemaker\imagemaker\bin\Debug\net461\gsdll32.dll");

            // Informationen über die Verwendete Methode der GhostScript DLL ins Log schreiben
            log.Info($"using (GhostscriptRasterizer rasterizer = new GhostscriptRasterizer()))");

            // Den Raterizer als Variable anlegen
            using (GhostscriptRasterizer rasterizer = new GhostscriptRasterizer())
            {
                // Die zu verarbeitende PDF Datei aus dem Stream holen
                rasterizer.Open(myBlob); //, gvi, true);

                // Für jede Seite in der PDF Datei muss eine eigene JPG Datei angelegt werden.
                for (int pageNumber = 1; pageNumber <= rasterizer.PageCount; pageNumber++)
                {
                    // 
                    log.Info($"_rasterizer.GetPage: {pageNumber}");
                    Image img = rasterizer.GetPage(desired_x_dpi, desired_y_dpi, pageNumber);

                    // Neuen Dateinamen zusammenbauen
                    string pageFileName = Path.Combine(name + "-Page-" + pageNumber.ToString() + ".jpg");
                    log.Info($"pageFileName: {pageFileName}");

                    log.Info($"GetBlockBlobReference");
                    MemoryStream imagestream = new MemoryStream();
                    img.Save(imagestream, ImageFormat.Jpeg);
                    imagestream.Seek(0, SeekOrigin.Begin);
                    CloudBlockBlob targetBlockBlob = targetContainer.GetBlockBlobReference(pageFileName);
                    targetBlockBlob.UploadFromStream(imagestream);
                }
            }
        }
    }
}
