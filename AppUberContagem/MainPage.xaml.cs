using Plugin.AdMob;
using Microsoft.Maui.Storage;
using System.Globalization;

namespace AppUberContagem;

public partial class MainPage : ContentPage
{
    // Variáveis para guardar os valores calculados na memória até o motorista "Aceitar"
    private double _ganhoBrutoAtual;
    private double _gastoUrbanoCalculado;
    private double _gastoMistoCalculado;
    private double _gastoRodoviarioCalculado;
    private bool _corridaFoiCalculada = false;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Verifica se o usuário é Premium e oculta o anúncio se for verdadeiro
        bool isPremium = Preferences.Default.Get("IsPremium", false);
        BottomBanner.IsVisible = !isPremium;
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

        // 6. Salva os dados na memória para o botão "Aceitar Corrida"
        _ganhoBrutoAtual = valorCorrida;
        _gastoUrbanoCalculado = gastoUrbano;
        _gastoMistoCalculado = gastoMisto;
        _gastoRodoviarioCalculado = gastoRodoviario;
        _corridaFoiCalculada = true;
    }

    private void BtnLimpar_Clicked(object sender, EventArgs e)
    {
        txtPrecoCorrida.Text = string.Empty;
        txtDistanciaAtePassageiro.Text = string.Empty;
        txtDistancia.Text = string.Empty;

        lblLitrosUrbano.Text = "0,00 L";
        lblGastoUrbano.Text = "R$ 0,00";
        lblLucroUrbano.Text = "R$ 0,00";
        lblLucroUrbano.TextColor = Colors.Green;

        lblLitrosMisto.Text = "0,00 L";
        lblGastoMisto.Text = "R$ 0,00";
        lblLucroMisto.Text = "R$ 0,00";
        lblLucroMisto.TextColor = Colors.Green;

        lblLitrosRodoviario.Text = "0,00 L";
        lblGastoRodoviario.Text = "R$ 0,00";
        lblLucroRodoviario.Text = "R$ 0,00";
        lblLucroRodoviario.TextColor = Colors.Green;

        _corridaFoiCalculada = false;
    }

    private void OnBannerFailedToLoad(object? sender, IAdError e)
    {
        BottomBanner.IsVisible = false;
    }

    // ==========================================
    // LÓGICA DO MODO PREMIUM - ACEITAR CORRIDA
    // ==========================================

    private async void OnCenarioUrbanoTapped(object sender, EventArgs e)
    {
        await ProcessarAceiteCorrida("Urbano", _gastoUrbanoCalculado);
    }

    private async void OnCenarioMistoTapped(object sender, EventArgs e)
    {
        await ProcessarAceiteCorrida("Misto", _gastoMistoCalculado);
    }

    private async void OnCenarioRodoviarioTapped(object sender, EventArgs e)
    {
        await ProcessarAceiteCorrida("Rodoviário", _gastoRodoviarioCalculado);
    }

    private async Task ProcessarAceiteCorrida(string cenario, double gastoCombustivel)
    {
        if (!_corridaFoiCalculada)
        {
            await DisplayAlert("Atenção", "Você precisa calcular a corrida primeiro.", "OK");
            return;
        }

        // 🛑 PERGUNTA DE CONFIRMAÇÃO ANTES DE ACEITAR E SALVAR
        bool confirmarAceite = await DisplayAlert("Confirmação", $"Deseja aceitar esta corrida no cenário {cenario}?", "Sim", "Não");
        if (!confirmarAceite)
        {
            return; // Interrompe se o motorista clicar em "Não"
        }

        bool isPremium = Preferences.Default.Get("IsPremium", false);

        if (!isPremium)
        {
            bool querAssinar = await DisplayAlert("Função Premium ⭐", "O registro automático de corridas para controle financeiro é uma função Premium. Deseja conhecer?", "Sim", "Agora não");
            if (querAssinar)
            {
                // Navega para a tela Premium
                await Navigation.PushAsync(new PremiumPage());
            }
            return;
        }

        // Puxa o serviço do banco de dados que criamos
        var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
        if (dbService == null) return;

        // 1. Salva o Ganho Bruto (Valor da corrida pago pelo passageiro)
        await dbService.SalvarRegistroAsync(new Models.RegistroFinanceiro
        {
            Data = DateTime.Now,
            TipoMovimentacao = "Ganho",
            Categoria = "Corrida de App",
            Descricao = $"Corrida Aceita ({cenario})",
            Valor = _ganhoBrutoAtual
        });

        // 2. Salva o Gasto (O custo do combustível daquela corrida)
        await dbService.SalvarRegistroAsync(new Models.RegistroFinanceiro
        {
            Data = DateTime.Now,
            TipoMovimentacao = "Gasto",
            Categoria = "Combustível",
            Descricao = $"Custo da Corrida ({cenario})",
            Valor = gastoCombustivel
        });

        // Feedback visual pro usuário
        await DisplayAlert("Sucesso! ✅", $"Corrida salva no seu relatório!\n\nGanho: R$ {_ganhoBrutoAtual:F2}\nGasto Combustível: R$ {gastoCombustivel:F2}", "OK");

        // Limpa a tela automaticamente após aceitar para ele fazer a próxima
        BtnLimpar_Clicked(this, EventArgs.Empty);
    }
}