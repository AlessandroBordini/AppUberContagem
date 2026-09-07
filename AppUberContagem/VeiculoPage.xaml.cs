using Microsoft.Maui.Storage;
using System.Globalization;
using AppUberContagem.Helpers;
using Plugin.AdMob;
using AppUberContagem.Services;

namespace AppUberContagem;

public partial class VeiculoPage : ContentPage
{
    public VeiculoPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        CarregarConfiguracoes();

        bool isPremium = await PremiumHelper.IsPremiumAsync();
        BottomBanner.IsVisible = !isPremium;

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
        int tipoIndex = Preferences.Default.Get("TipoVeiculoIndex", 0);
        pckTipoVeiculo.SelectedIndex = tipoIndex;
        AtualizarSecaoVeiculo(tipoIndex);

        txtKmGasolina.Text = Preferences.Default.Get("KmGasolina", string.Empty);
        txtKmAlcool.Text = Preferences.Default.Get("KmAlcool", string.Empty);
        txtAutonomiaCarga.Text = Preferences.Default.Get("AutonomiaCarga", string.Empty);
        txtCapacidadeTanque.Text = Preferences.Default.Get("CapacidadeTanque", string.Empty);
    }

    private void PckTipoVeiculo_SelectedIndexChanged(object sender, EventArgs e) => AtualizarSecaoVeiculo(pckTipoVeiculo.SelectedIndex);

    private void AtualizarSecaoVeiculo(int tipoIndex)
    {
        vslConsumoCombustao.IsVisible = (tipoIndex == 0);
        vslConsumoEletrico.IsVisible = (tipoIndex == 1);
    }

    private async void BtnSalvarVeiculo_Clicked(object sender, EventArgs e)
    {
        int tipoVeiculo = pckTipoVeiculo.SelectedIndex;
        Preferences.Default.Set("TipoVeiculoIndex", tipoVeiculo);

        if (tipoVeiculo == 0)
        {
            Preferences.Default.Set("KmGasolina", txtKmGasolina.Text ?? string.Empty);
            Preferences.Default.Set("KmAlcool", txtKmAlcool.Text ?? string.Empty);
            Preferences.Default.Set("CapacidadeTanque", txtCapacidadeTanque.Text ?? string.Empty);
        }
        else
        {
            Preferences.Default.Set("AutonomiaCarga", txtAutonomiaCarga.Text ?? string.Empty);
        }

        // --- VALIDAÇÃO CRUZADA: VERIFICA SE O COMBUSTÍVEL ESTÁ FALTANDO ---
        if (tipoVeiculo == 0) // Combustão
        {
            bool usaGasolina = Preferences.Default.Get("UsaGasolina", true);
            string strPrecoGas = Preferences.Default.Get("PrecoGasolina", "0");
            string strPrecoAlc = Preferences.Default.Get("PrecoAlcool", "0");

            double.TryParse(strPrecoGas.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double precoGas);
            double.TryParse(strPrecoAlc.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double precoAlc);

            double precoAtual = usaGasolina ? precoGas : precoAlc;

            if (precoAtual <= 0)
            {
                bool irParaCombustivel = await DisplayAlert(
                    "Falta o Preço do Combustível 💰",
                    "Veículo salvo com sucesso! Agora precisamos do preço atual do combustível para calcular suas corridas.",
                    "Cadastrar Preço",
                    "Mais Tarde"
                );

                if (irParaCombustivel)
                {
                    await Navigation.PushAsync(new CombustivelPage());
                    return;
                }
            }
        }
        else // Elétrico
        {
            string precoCargaStr = Preferences.Default.Get("PrecoCarga", "0");
            double.TryParse(precoCargaStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double precoCarga);

            if (precoCarga <= 0)
            {
                bool irParaCombustivel = await DisplayAlert(
                    "Falta o Preço da Carga ⚡",
                    "Veículo salvo com sucesso! Agora informe o preço da carga elétrica para prosseguir.",
                    "Cadastrar Preço",
                    "Mais Tarde"
                );

                if (irParaCombustivel)
                {
                    await Navigation.PushAsync(new CombustivelPage());
                    return;
                }
            }
        }

        await DisplayAlert("Sucesso", "Informações do veículo salvas com sucesso!", "OK");
        await Navigation.PopAsync();
    }
}