using SnipMasterLib;
using System.Windows;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace SnipmasterDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SnippingTool _snippingTool;

        public MainWindow()
        {
            InitializeComponent();
            _snippingTool = new SnippingTool();
            _snippingTool.OnSnipCompleted += OnSnipCompleted;
        }

        /// <summary>
        /// Event handler for the snipping tool button click.
        /// </summary>
        private void btnSnip_Click(object sender, RoutedEventArgs e)
        {
            _snippingTool.StartSnipping();
        }

        /// <summary>
        /// Callback method that is invoked when the snip is completed.
        /// Displays the snipped image in the WPF Image control.
        /// </summary>
        /// <param name="snippedImage">The snipped image captured from the screen.</param>
        private void OnSnipCompleted(BitmapImage snippedImage)
        {
            imgSnipped.Source = snippedImage;
        }
    }
}