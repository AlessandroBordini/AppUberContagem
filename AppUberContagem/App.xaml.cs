using AppUberContagem.Services;

namespace AppUberContagem
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Inicia o aplicativo exibindo a SplashPage
            var window = new Window(new SplashPage());

            // Preload interstitial as soon as the UI is up so it's ready when needed.
            window.Created += (_, _) =>
            {
                try
                {
                    var ads = Current?.Handler?.MauiContext?.Services.GetService<InterstitialAdManager>()
                        ?? IPlatformApplication.Current?.Services.GetService<InterstitialAdManager>();
                    ads?.Preload();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Interstitial preload failed: {ex.Message}");
                }
            };

            return window;
        }
    }
}