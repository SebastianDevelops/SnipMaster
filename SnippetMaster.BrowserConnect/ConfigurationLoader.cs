using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnippetMaster.BrowserConnect
{
    public class ConfigurationLoader()
    {
        public Settings LoadSettings()
        {
            try
            {
                var check = Directory.GetCurrentDirectory();

                var configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("appsettings.dev.json", optional: true, reloadOnChange: true);

                var configuration = configurationBuilder.Build();

                var appSettings = configuration.Get<Settings>();
                return appSettings;
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                throw;
            }
            
        }
    }
}
