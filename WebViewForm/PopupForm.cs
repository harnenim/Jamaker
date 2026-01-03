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

            CoreWebView2 wv = mainView.CoreWebView2;
            wv.DocumentTitleChanged += (s, e) => { try { Text = wv.DocumentTitle; } catch { } };
            wv.WindowCloseRequested += (s, e) => { try { Close(); } catch { } };
            wv.Settings.AreDevToolsEnabled = showDevTools;
            wv.Settings.AreBrowserAcceleratorKeysEnabled = showDevTools;
            wv.Settings.IsStatusBarEnabled = false;
        }
    }
}
