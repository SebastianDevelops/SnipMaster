using Microsoft.Extensions.Configuration;
using PinataNET;
using QRCoder;
using static QRCoder.PayloadGenerator;

namespace SnippetMaster.BrowserConnect
{
    public static class FileService
    {
        private readonly static string _pinataApiKey;
        private readonly static string _pinataApiSecretKey;
        private readonly static string _jwt;
        private static IConfiguration _config;

        static FileService()
        {
            var configLoader = new ConfigurationLoader();
            var settings = configLoader.LoadSettings();

            _pinataApiKey = settings.PinataApiKey;
            _pinataApiSecretKey = settings.PinataApiSecretKey;
            _jwt = settings.JWT;
        }

        public static async Task<string> UploadFile(string tempFileName)
        {
            try
            {
                var client = new PinataClient(_jwt);

                var response = await client.PinFileToIPFSAsync(tempFileName);

                var ipfsHash = response.IpfsHash;

                return $"https://gateway.pinata.cloud/ipfs/{ipfsHash}";
            }
            catch (Exception ex)
            {
                throw new Exception("Something went wrong : " + ex.Message);
            }
            finally
            {
                if (File.Exists(tempFileName))
                {
                    File.Delete(tempFileName);
                }
            }
        }

        public static byte[] GenerateQrFromLink(string snipFileUrl)
        {
            var url = new Url(snipFileUrl);

            using (var qrGenerator = new QRCodeGenerator())
            {
                using (var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
                {
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        // Convert QR code to bitmap
                        var bitmap = qrCode.GetGraphic(20);

                        using (var memoryStream = new MemoryStream(bitmap))
                        {
                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }
    }
}
