using Microsoft.Extensions.Logging;
using Plugin.AdMob;
using Plugin.AdMob.Configuration;
using AppUberContagem.Services;

namespace AppUberContagem
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseAdMob()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            AdConfig.DefaultInterstitialAdUnitId = AdMobIds.InterstitialAdUnitId;
            AdConfig.DefaultBannerAdUnitId = AdMobIds.BannerAdUnitId;

            builder.Services.AddSingleton<InterstitialAdManager>();

#if DEBUG
            // Routes all ad requests to Google's safe test creatives.
            // Never ship with this enabled against live ads you might click.
            AdConfig.UseTestAdUnitIds = true;
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
