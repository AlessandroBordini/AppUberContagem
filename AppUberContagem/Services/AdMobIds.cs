namespace AppUberContagem.Services;

/// <summary>
/// AdMob application and ad unit IDs.
/// </summary>
public static class AdMobIds
{
    // Google App ID
    public const string AndroidAppId = "ca-app-pub-1377613459651878~5674210062";

    // Banner Ad Unit ID
    public const string BannerAdUnitId =
#if ANDROID
        "ca-app-pub-1377613459651878/9586223279";
#elif IOS
        "ca-app-pub-3940256099942544/2934735716";
#else
        "ca-app-pub-3940256099942544/6300978111";
#endif

    // Interstitial Ad Unit ID (Tela Cheia)
    public const string InterstitialAdUnitId =
#if ANDROID
        "ca-app-pub-1377613459651878/4562703713";
#elif IOS
        "ca-app-pub-3940256099942544/4411168110";
#else
        "ca-app-pub-3940256099942544/1033173712";
#endif
}