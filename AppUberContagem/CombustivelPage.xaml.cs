using Microsoft.Maui.Storage;
using Plugin.AdMob;
using AppUberContagem.Services;
using AppUberContagem.Helpers;

namespace AppUberContagem;

public partial class CombustivelPage : ContentPage
{
    public CombustivelPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        CarregarConfiguracoes();

        // Utiliza o PremiumHelper seguro em vez de checagem solta
        bool isPremium = await PremiumHelper.IsPremiumAsync();

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
        // Carrega dados de combustíveis tradicionais
        txtKmGasolina.Text = Preferences.Default.Get("KmGasolina", string.Empty);
        txtKmAlcool.Text = Preferences.Default.Get("KmAlcool", string.Empty);
        txtPrecoGasolina.Text = Preferences.Default.Get("PrecoGasolina", string.Empty);
        txtPrecoAlcool.Text = Preferences.Default.Get("PrecoAlcool", string.Empty);

        // Carrega dados de veículos elétricos
        txtPrecoCarga.Text = Preferences.Default.Get("PrecoCarga", string.Empty);
        txtAutonomiaCarga.Text = Preferences.Default.Get("AutonomiaCarga", string.Empty);

        // Tipo de veículo selecionado (0 = Combustível, 1 = Elétrico)
        int tipoIndex = Preferences.Default.Get("TipoVeiculoIndex", 0);
        pckTipoVeiculo.SelectedIndex = tipoIndex;

        bool usaGasolina = Preferences.Default.Get("UsaGasolina", true);
        rbGasolina.IsChecked = usaGasolina;
        rbAlcool.IsChecked = !usaGasolina;

        AtualizarVisibilidadeCampos(usaGasolina);
        AtualizarSecaoVeiculo(tipoIndex);
    }

    private void PckTipoVeiculo_SelectedIndexChanged(object sender, EventArgs e)
    {
        AtualizarSecaoVeiculo(pckTipoVeiculo.SelectedIndex);
    }

    private void AtualizarSecaoVeiculo(int tipoIndex)
    {
        bool ehEletrico = (tipoIndex == 1);

        vslSecaoCombustivel.IsVisible = !ehEletrico;
        vslSecaoEletrico.IsVisible = ehEletrico;
    }

    private void RbCombustivel_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            AtualizarVisibilidadeCampos(rbGasolina.IsChecked);
        }
    }

    private void AtualizarVisibilidadeCampos(bool ehGasolina)
    {
        lblKmGasolina.IsVisible = ehGasolina;
        txtKmGasolina.IsVisible = ehGasolina;
        lblPrecoGasolina.IsVisible = ehGasolina;
        txtPrecoGasolina.IsVisible = ehGasolina;

        lblKmAlcool.IsVisible = !ehGasolina;
        txtKmAlcool.IsVisible = !ehGasolina;
        lblPrecoAlcool.IsVisible = !ehGasolina;
        txtPrecoAlcool.IsVisible = !ehGasolina;
    }

    private async void BtnSalvar_Clicked(object sender, EventArgs e)
    {
        int tipoIndex = pckTipoVeiculo.SelectedIndex;
        decimal custoPorKm = 0;

        if (tipoIndex == 1) // Elétrico
        {
            if (string.IsNullOrWhiteSpace(txtPrecoCarga.Text) || string.IsNullOrWhiteSpace(txtAutonomiaCarga.Text))
            {
                await DisplayAlert("Atenção", "Por favor, preencha o custo da carga e a autonomia para salvar.", "OK");
                return;
            }

            Preferences.Default.Set("PrecoCarga", txtPrecoCarga.Text ?? string.Empty);
            Preferences.Default.Set("AutonomiaCarga", txtAutonomiaCarga.Text ?? string.Empty);

            if (decimal.TryParse(txtPrecoCarga.Text, out decimal precoCarga) &&
                decimal.TryParse(txtAutonomiaCarga.Text, out decimal autonomiaCarga) &&
                autonomiaCarga > 0)
            {
                custoPorKm = precoCarga / autonomiaCarga;
            }
        }
        else // Combustível (Gasolina/Álcool)
        {
            bool ehGasolina = rbGasolina.IsChecked;

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

            string consumoStr = ehGasolina ? txtKmGasolina.Text : txtKmAlcool.Text;
            string precoStr = ehGasolina ? txtPrecoGasolina.Text : txtPrecoAlcool.Text;

            if (decimal.TryParse(consumoStr, out decimal consumo) &&
                decimal.TryParse(precoStr, out decimal preco) &&
                consumo > 0)
            {
                custoPorKm = preco / consumo;
            }
        }

        Preferences.Default.Set("TipoVeiculoIndex", tipoIndex);
        Preferences.Default.Set("CustoPorKm", (double)custoPorKm);

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
            Preferences.Default.Remove("PrecoCarga");
            Preferences.Default.Remove("AutonomiaCarga");
            Preferences.Default.Remove("TipoVeiculoIndex");
            Preferences.Default.Remove("CustoPorKm");

            txtKmGasolina.Text = string.Empty;
            txtKmAlcool.Text = string.Empty;
            txtPrecoGasolina.Text = string.Empty;
            txtPrecoAlcool.Text = string.Empty;
            txtPrecoCarga.Text = string.Empty;
            txtAutonomiaCarga.Text = string.Empty;

            pckTipoVeiculo.SelectedIndex = 0;
            rbGasolina.IsChecked = true;

            await DisplayAlert("Pronto", "Dados limpos com sucesso!", "OK");
        }
    }

    private void OnBannerFailedToLoad(object? sender, IAdError e)
    {
        BottomBanner.IsVisible = false;
    }
}