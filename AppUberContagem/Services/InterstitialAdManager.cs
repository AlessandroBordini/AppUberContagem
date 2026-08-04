using Plugin.AdMob;
using Plugin.AdMob.Configuration;
using Plugin.AdMob.Services;

namespace AppUberContagem.Services;

/// <summary>
/// Preloads and shows AdMob interstitial (fullscreen) ads.
/// Load is asynchronous — never call Show immediately after Load.
/// </summary>
public sealed class InterstitialAdManager
{
    private readonly IInterstitialAdService _service;
    private IInterstitialAd? _ad;
    private bool _showWhenLoaded;
    private bool _isLoading;

    public InterstitialAdManager(IInterstitialAdService service)
    {
        _service = service;
    }

    /// <summary>Starts loading an interstitial in the background.</summary>
    public void Preload()
    {
        if (_ad?.IsLoaded == true || _isLoading)
            return;

        Detach(_ad);

        _ad = _service.CreateAd(GetAdUnitId());
        _ad.OnAdLoaded += OnAdLoaded;
        _ad.OnAdFailedToLoad += OnAdFailedToLoad;
        _ad.OnAdDismissed += OnAdDismissed;
        _ad.OnAdFailedToShow += OnAdFailedToShow;

        _isLoading = true;
        _ad.Load();
    }

    /// <summary>
    /// Shows a loaded interstitial, or waits for the current load and shows it when ready.
    /// </summary>
    public void Show(bool showWhenReady = true)
    {
        if (_ad?.IsLoaded == true)
        {
            _showWhenLoaded = false;
            _ad.Show();
            return;
        }

        _showWhenLoaded = showWhenReady;
        Preload();
    }

    private void OnAdLoaded(object? sender, EventArgs e)
    {
        _isLoading = false;

        if (!_showWhenLoaded)
            return;

        _showWhenLoaded = false;
        _ad?.Show();
    }

    private void OnAdFailedToLoad(object? sender, IAdError e)
    {
        _isLoading = false;
        _showWhenLoaded = false;
        System.Diagnostics.Debug.WriteLine($"Interstitial failed to load: {e}");
    }

    private void OnAdFailedToShow(object? sender, IAdError e)
    {
        _showWhenLoaded = false;
        System.Diagnostics.Debug.WriteLine($"Interstitial failed to show: {e}");
        Preload();
    }

    private void OnAdDismissed(object? sender, EventArgs e)
    {
        // Ad instance is single-use; preload the next one after dismiss.
        Detach(_ad);
        _ad = null;
        _isLoading = false;
        Preload();
    }

    private void Detach(IInterstitialAd? ad)
    {
        if (ad is null)
            return;

        ad.OnAdLoaded -= OnAdLoaded;
        ad.OnAdFailedToLoad -= OnAdFailedToLoad;
        ad.OnAdDismissed -= OnAdDismissed;
        ad.OnAdFailedToShow -= OnAdFailedToShow;
    }

    private static string? GetAdUnitId()
    {
        // When UseTestAdUnitIds is true, the plugin replaces any ID with Google's test units.
        return AdConfig.UseTestAdUnitIds ? null : AdMobIds.InterstitialAdUnitId;
    }
}
