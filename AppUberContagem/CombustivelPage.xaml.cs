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

        // Verifica se o usuário é assinante Premium
        bool isPremium = Preferences.Default.Get("IsPremium", false);

        // Controla a visibilidade do banner inferior
        BottomBanner.IsVisible = !isPremium;

        // Se não for premium, exibe o anúncio de tela cheia
        if (!isPremium)
        {
            MostrarAnuncioTelaCheia();
        }
    }

    private void MostrarAnuncioTelaCheia()
    {
        try
        {
            var ads = Handler?.MauiContext?.Services.GetService<InterstitialAdManager>()
                ?? IPlatformApplication.Current?.Services.GetService<InterstitialAdManager>();

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

        // Garante que o estado visual dos campos reflita a configuração carregada
        txtKmGasolina.IsEnabled = usaGasolina;
        txtPrecoGasolina.IsEnabled = usaGasolina;
        txtKmAlcool.IsEnabled = !usaGasolina;
        txtPrecoAlcool.IsEnabled = !usaGasolina;
    }

    private void RbCombustivel_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            bool ehGasolina = rbGasolina.IsChecked;

            // Ativa/desativa os campos conforme a escolha do RadioButton
            txtKmGasolina.IsEnabled = ehGasolina;
            txtPrecoGasolina.IsEnabled = ehGasolina;

            txtKmAlcool.IsEnabled = !ehGasolina;
            txtPrecoAlcool.IsEnabled = !ehGasolina;
        }
    }

    private async void BtnSalvar_Clicked(object sender, EventArgs e)
    {
        bool ehGasolina = rbGasolina.IsChecked;

        // Valida apenas os campos do combustível selecionado
        if (ehGasolina)
        {
            if (string.IsNullOrWhiteSpace(txtKmGasolina.Text) || string.IsNullOrWhiteSpace(txtPrecoGasolina.Text))
            {
                await DisplayAlert("Atenção", "Por favor, preencha o consumo e o preço da Gasolina para salvar.", "OK");
                return;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(txtKmAlcool.Text) || string.IsNullOrWhiteSpace(txtPrecoAlcool.Text))
            {
                await DisplayAlert("Atenção", "Por favor, preencha o consumo e o preço do Álcool para salvar.", "OK");
                return;
            }
        }

        Preferences.Default.Set("KmGasolina", txtKmGasolina.Text ?? string.Empty);
        Preferences.Default.Set("KmAlcool", txtKmAlcool.Text ?? string.Empty);
        Preferences.Default.Set("PrecoGasolina", txtPrecoGasolina.Text ?? string.Empty);
        Preferences.Default.Set("PrecoAlcool", txtPrecoAlcool.Text ?? string.Empty);
        Preferences.Default.Set("UsaGasolina", ehGasolina);

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