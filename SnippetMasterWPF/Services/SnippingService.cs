using SnipMasterLib;
using SnipMasterLib.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SnippetMasterWPF.Services
{
    public class SnippingService : ISnippingService
    {
        private readonly SnippingTool _snippingTool;

        public SnippingService()
        {
            _snippingTool = new SnippingTool();
            _snippingTool.OnSnipCompleted += SnippingTool_OnSnipCompleted;
        }

        public event Action<BitmapImage> OnSnipCompleted;

        public void StartSnipping()
        {
            _snippingTool.StartSnipping();
        }

        private void SnippingTool_OnSnipCompleted(BitmapImage snippedImage)
        {
            // Directly invoke the event with the BitmapImage
            OnSnipCompleted?.Invoke(snippedImage);
        }
    }

    public interface ISnippingService
    {
        void StartSnipping();
        event Action<BitmapImage> OnSnipCompleted;
    }
}
