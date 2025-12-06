using Microsoft.Web.WebView2.Core;
using System.Runtime.Versioning;

namespace WebViewForm
{
    [SupportedOSPlatform("windows")]
    public partial class PopupForm : Form
    {
        public PopupForm(CoreWebView2Environment env)
        {
            InitializeComponent();
            InitWebView(env);
        }

        private static readonly bool showDevTools = File.Exists(Path.Combine(Application.StartupPath, $"setting/.ShowDevTools"));
        public bool fixedUrl = false;

        private async void InitWebView(CoreWebView2Environment env)
        {
            mainView.NavigationStarting += (s, e) => { Activate(); };
            
            await mainView.EnsureCoreWebView2Async(env);
            mainView.CoreWebView2.DocumentTitleChanged += (s, e) => { try { Text = mainView.CoreWebView2.DocumentTitle; } catch { } };
            mainView.CoreWebView2.WindowCloseRequested += (s, e) => { try { Close(); } catch { } };
            mainView.CoreWebView2.Settings.AreDevToolsEnabled = showDevTools;
            mainView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = showDevTools;
        }
    }
}
