using Plugin.AdMob;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace AppUberContagem;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void BtnCalcular_Clicked(object sender, EventArgs e)
    {
        // 1. Verifica se os campos estão preenchidos
        if (string.IsNullOrWhiteSpace(txtPrecoCorrida.Text) ||
            string.IsNullOrWhiteSpace(txtDistanciaAtePassageiro.Text) ||
            string.IsNullOrWhiteSpace(txtDistancia.Text))
        {
            await DisplayAlert("Atenção", "Preencha todos os campos da corrida para calcular.", "OK");
            return;
        }

        // 2. Conversão segura
        if (!double.TryParse(txtPrecoCorrida.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double valorCorrida) ||
            !double.TryParse(txtDistanciaAtePassageiro.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double distAtePassageiro) ||
            !double.TryParse(txtDistancia.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double distCorrida))
        {
            await DisplayAlert("Erro", "Digite apenas números válidos nos campos.", "OK");
            return;
        }

        // 3. Pega dados salvos
        bool usaGasolina = Preferences.Default.Get("UsaGasolina", true);
        string strKmGas = Preferences.Default.Get("KmGasolina", "0");
        string strKmAlc = Preferences.Default.Get("KmAlcool", "0");
        string strPrecoGas = Preferences.Default.Get("PrecoGasolina", "0");
        string strPrecoAlc = Preferences.Default.Get("PrecoAlcool", "0");

        double.TryParse(strKmGas.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double kmGas);
        double.TryParse(strKmAlc.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double kmAlc);
        double.TryParse(strPrecoGas.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double precoGas);
        double.TryParse(strPrecoAlc.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double precoAlc);

        double kmPorLitroBase = usaGasolina ? kmGas : kmAlc;
        double precoLitro = usaGasolina ? precoGas : precoAlc;

        if (kmPorLitroBase <= 0)
        {
            await DisplayAlert("Falta Configuração", "Por favor, vá até a aba de Combustível e salve o consumo (Km/L).", "OK");
            return;
        }

        // 4. Proporções por Cenário
        double kmUrbano = kmPorLitroBase;
        double kmMisto = kmPorLitroBase * 1.15;
        double kmRodoviario = kmPorLitroBase * 1.30;

        double totalKm = distAtePassageiro + distCorrida;

        // --- Cenário Urbano ---
        double litrosUrbano = totalKm / kmUrbano;
        double gastoUrbano = litrosUrbano * precoLitro;
        double lucroUrbano = valorCorrida - gastoUrbano;

        // --- Cenário Misto ---
        double litrosMisto = totalKm / kmMisto;
        double gastoMisto = litrosMisto * precoLitro;
        double lucroMisto = valorCorrida - gastoMisto;

        // --- Cenário Rodoviário ---
        double litrosRodoviario = totalKm / kmRodoviario;
        double gastoRodoviario = litrosRodoviario * precoLitro;
        double lucroRodoviario = valorCorrida - gastoRodoviario;

        // 5. Exibe resultados na interface
        // Urbano
        lblLitrosUrbano.Text = $"{litrosUrbano:F2} L";
        lblGastoUrbano.Text = $"R$ {gastoUrbano:F2}";
        lblLucroUrbano.Text = $"R$ {lucroUrbano:F2}";
        lblLucroUrbano.TextColor = lucroUrbano >= 0 ? Colors.Green : Colors.Red;

        // Misto
        lblLitrosMisto.Text = $"{litrosMisto:F2} L";
        lblGastoMisto.Text = $"R$ {gastoMisto:F2}";
        lblLucroMisto.Text = $"R$ {lucroMisto:F2}";
        lblLucroMisto.TextColor = lucroMisto >= 0 ? Colors.Green : Colors.Red;

        // Rodoviário
        lblLitrosRodoviario.Text = $"{litrosRodoviario:F2} L";
        lblGastoRodoviario.Text = $"R$ {gastoRodoviario:F2}";
        lblLucroRodoviario.Text = $"R$ {lucroRodoviario:F2}";
        lblLucroRodoviario.TextColor = lucroRodoviario >= 0 ? Colors.Green : Colors.Red;
    }

    private void BtnLimpar_Clicked(object sender, EventArgs e)
    {
        txtPrecoCorrida.Text = string.Empty;
        txtDistanciaAtePassageiro.Text = string.Empty;
        txtDistancia.Text = string.Empty;

        // Limpa Urbano
        lblLitrosUrbano.Text = "0,00 L";
        lblGastoUrbano.Text = "R$ 0,00";
        lblLucroUrbano.Text = "R$ 0,00";
        lblLucroUrbano.TextColor = Colors.Green;

        // Limpa Misto
        lblLitrosMisto.Text = "0,00 L";
        lblGastoMisto.Text = "R$ 0,00";
        lblLucroMisto.Text = "R$ 0,00";
        lblLucroMisto.TextColor = Colors.Green;

        // Limpa Rodoviário
        lblLitrosRodoviario.Text = "0,00 L";
        lblGastoRodoviario.Text = "R$ 0,00";
        lblLucroRodoviario.Text = "R$ 0,00";
        lblLucroRodoviario.TextColor = Colors.Green;
    }

    private void OnBannerFailedToLoad(object? sender, IAdError e)
    {
        // Hide the empty banner slot when no ad is available. (INTACTO)
        BottomBanner.IsVisible = false;
    }
}