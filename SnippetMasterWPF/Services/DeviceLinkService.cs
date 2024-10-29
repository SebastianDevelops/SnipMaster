using SnippetMaster.BrowserConnect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace SnippetMasterWPF.Services
{
    public class DeviceLinkService : IDeviceLinkService
    {

        /// <summary>
        /// Creates a temporary text file with the specified content.
        /// </summary>
        /// <param name="snipText">The content to write to the text file.</param>
        /// <returns>A url to the file</returns>
        public async Task<KeyValuePair<string, byte[]>> GenerateDeviceLink(string snipText)
        {
            try
            {
                string tempFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");

                File.WriteAllText(tempFileName, snipText);

                string snipFileUrl = await FileService.UploadFile(tempFileName);
                byte[] snipFileQr = FileService.GenerateQrFromLink(snipFileUrl);

                return new KeyValuePair<string, byte[]>(snipFileUrl, snipFileQr);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while getting qr code: "+ ex.Message, "Error");

                throw;
            }
            
        }
    }

    public interface IDeviceLinkService
    {
        Task<KeyValuePair<string, byte[]>> GenerateDeviceLink(string snipText);
    }
}
