using Microsoft.Maui.Storage;
using Plugin.AdMob;
using AppUberContagem.Services;

namespace AppUberContagem;

public partial class CombustivelPage : ContentPage
{
    public CombustivelPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CarregarConfiguracoes();
        MostrarAnuncioTelaCheia();
    }

    private void MostrarAnuncioTelaCheia()
    {
        try
        {
            var ads = Handler?.MauiContext?.Services.GetService<InterstitialAdManager>()
                ?? IPlatformApplication.Current?.Services.GetService<InterstitialAdManager>();

            // Shows immediately if already preloaded; otherwise waits for OnAdLoaded then shows.
            ads?.Show(showWhenReady: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Interstitial show failed: {ex.Message}");
        }
    }

    private void CarregarConfiguracoes()
    {
        txtKmGasolina.Text = Preferences.Default.Get("KmGasolina", string.Empty);
        txtKmAlcool.Text = Preferences.Default.Get("KmAlcool", string.Empty);
        txtPrecoGasolina.Text = Preferences.Default.Get("PrecoGasolina", string.Empty);
        txtPrecoAlcool.Text = Preferences.Default.Get("PrecoAlcool", string.Empty);

        bool usaGasolina = Preferences.Default.Get("UsaGasolina", true);
        rbGasolina.IsChecked = usaGasolina;
        rbAlcool.IsChecked = !usaGasolina;
    }

    private async void BtnSalvar_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtKmGasolina.Text) || string.IsNullOrWhiteSpace(txtPrecoGasolina.Text))
        {
            await DisplayAlert("Atenção", "Por favor, preencha os campos de consumo e preço para salvar.", "OK");
            return;
        }

        Preferences.Default.Set("KmGasolina", txtKmGasolina.Text ?? string.Empty);
        Preferences.Default.Set("KmAlcool", txtKmAlcool.Text ?? string.Empty);
        Preferences.Default.Set("PrecoGasolina", txtPrecoGasolina.Text ?? string.Empty);
        Preferences.Default.Set("PrecoAlcool", txtPrecoAlcool.Text ?? string.Empty);
        Preferences.Default.Set("UsaGasolina", rbGasolina.IsChecked);

        await DisplayAlert("Sucesso", "Configuração salva!", "OK");
    }

    private async void BtnLimpar_Clicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Limpar Dados",
            "Tem certeza de que deseja apagar todas as configurações salvas?",
            "Sim",
            "Não");

        if (confirmar)
        {
            Preferences.Default.Remove("KmGasolina");
            Preferences.Default.Remove("KmAlcool");
            Preferences.Default.Remove("PrecoGasolina");
            Preferences.Default.Remove("PrecoAlcool");
            Preferences.Default.Remove("UsaGasolina");

            txtKmGasolina.Text = string.Empty;
            txtKmAlcool.Text = string.Empty;
            txtPrecoGasolina.Text = string.Empty;
            txtPrecoAlcool.Text = string.Empty;

            rbGasolina.IsChecked = true;

            await DisplayAlert("Pronto", "Dados limpos com sucesso!", "OK");
        }
    }

    private void OnBannerFailedToLoad(object? sender, IAdError e)
    {
        BottomBanner.IsVisible = false;
    }
}
