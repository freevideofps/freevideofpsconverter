#region

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

#endregion

namespace FreeVideoFPSConverter
{
    /// <summary>
    ///     Interaction logic for UsageWindow.xaml
    /// </summary>
    public partial class UsageWindow
    {
        public UsageWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            const string resourceName = "FreeVideoFPSConverter.Templates.Usage.rtf";

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    RtbGpl.Selection.Load(stream, DataFormats.Rtf);
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the ButtonOk control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        ///     Handles the click on hyperlink.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        private void HandleClickOnHyperlink(object sender, MouseButtonEventArgs e)
        {
            Hyperlink hyperlink = sender as Hyperlink;

            if (hyperlink != null)
            {
                Process.Start(hyperlink.NavigateUri.ToString());
            }
        }
    }
}