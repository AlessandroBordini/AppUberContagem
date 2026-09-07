using Plugin.AdMob;
using Microsoft.Maui.Storage;
using System.Globalization;
using AppUberContagem.Helpers;

namespace AppUberContagem;

public partial class MainPage : ContentPage
{
    // Variáveis para guardar os valores calculados na memória até o motorista "Aceitar"
    private double _ganhoBrutoAtual;
    private double _gastoUrbanoCalculado;
    private double _gastoMistoCalculado;
    private double _gastoRodoviarioCalculado;
    private bool _corridaFoiCalculada = false;
    private bool _isRedirecting = false;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isRedirecting) return;

        // Utiliza o PremiumHelper seguro
        bool isPremium = await PremiumHelper.IsPremiumAsync();
        BottomBanner.IsVisible = !isPremium;

        // Valida se as configurações iniciais existem através de pop-ups bloqueantes
        await VerificarConfiguracoesIniciaisAsync();
    }

    private async Task VerificarConfiguracoesIniciaisAsync()
    {
        // 1. Verifica se o tipo de veículo foi definido
        int tipoVeiculo = Preferences.Default.Get("TipoVeiculoIndex", -1);
        if (tipoVeiculo == -1)
        {
            await ExibirAlertaRedirecionamentoAsync(
                "Configuração Inicial 🚗",
                "Você ainda não configurou o seu veículo. Cadastre as informações para prosseguir.",
                typeof(VeiculoPage)
            );
            return;
        }

        // 2. Valida conforme o tipo de veículo
        if (tipoVeiculo == 0) // Combustão
        {
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
                await ExibirAlertaRedirecionamentoAsync(
                    "Rendimento Pendente ⛽",
                    "Precisamos saber o rendimento (Km/L) do seu veículo para calcular os custos. Vá até a aba de veículo ou combustível para cadastrar.",
                    typeof(VeiculoPage)
                );
                return;
            }

            if (precoLitro <= 0)
            {
                await ExibirAlertaRedirecionamentoAsync(
                    "Preço do Combustível Ausente 💰",
                    "O preço do combustível não foi informado. Cadastre para continuar.",
                    typeof(CombustivelPage)
                );
                return;
            }
        }
        else // Elétrico
        {
            string autonomiaStr = Preferences.Default.Get("AutonomiaCarga", "0");
            string precoCargaStr = Preferences.Default.Get("PrecoCarga", "0");

            double.TryParse(autonomiaStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double autonomia);
            double.TryParse(precoCargaStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double precoCarga);

            if (autonomia <= 0 || precoCarga <= 0)
            {
                await ExibirAlertaRedirecionamentoAsync(
                    "Dados do Veículo Elétrico ⚡",
                    "Faltam informações de autonomia ou preço da carga para prosseguir.",
                    typeof(CombustivelPage)
                );
                return;
            }
        }
    }

    private async Task ExibirAlertaRedirecionamentoAsync(string titulo, string mensagem, Type paginaDestino)
    {
        _isRedirecting = true;

        bool irParaCadastro = await DisplayAlert(
            titulo,
            mensagem,
            "Cadastrar Agora",
            "Cancelar"
        );

        if (irParaCadastro)
        {
            if (Activator.CreateInstance(paginaDestino) is Page pagina)
            {
                await Navigation.PushAsync(pagina);
            }
        }

        _isRedirecting = false;
    }

    private async void BtnCalcular_Clicked(object sender, EventArgs e)
    {
        // 1. Verifica se os campos da corrida estão preenchidos
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

        // GARANTE QUE O CÁLCULO SÓ RODE SE TIVER O PREÇO E O KM/L
        if (kmPorLitroBase <= 0 || precoLitro <= 0)
        {
            await DisplayAlert("Falta Configuração", "Por favor, vá até a aba de Combustível e preencha o consumo (Km/L) e o preço para calcular.", "OK");
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

        // 5. Exibe resultados nos labels do Pop-up
        lblLitrosUrbano.Text = $"{litrosUrbano:F2} L";
        lblGastoUrbano.Text = $"R$ {gastoUrbano:F2}";
        lblLucroUrbano.Text = $"R$ {lucroUrbano:F2}";
        lblLucroUrbano.TextColor = lucroUrbano >= 0 ? Colors.Green : Colors.Red;

        lblLitrosMisto.Text = $"{litrosMisto:F2} L";
        lblGastoMisto.Text = $"R$ {gastoMisto:F2}";
        lblLucroMisto.Text = $"R$ {lucroMisto:F2}";
        lblLucroMisto.TextColor = lucroMisto >= 0 ? Colors.Green : Colors.Red;

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

        // 7. Torna o Pop-up visível na tela
        PopupCenarios.IsVisible = true;
    }

    private void BtnFecharPopup_Clicked(object sender, EventArgs e)
    {
        PopupCenarios.IsVisible = false;
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
        PopupCenarios.IsVisible = false;
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

        bool confirmarAceite = await DisplayAlert("Confirmação", $"Deseja aceitar esta corrida no cenário {cenario}?", "Sim", "Não");
        if (!confirmarAceite)
        {
            return;
        }

        bool isPremium = await PremiumHelper.IsPremiumAsync();

        if (!isPremium)
        {
            bool querAssinar = await DisplayAlert("Função Premium ⭐", "O registro automático de corridas para controle financeiro é uma função Premium. Deseja conhecer?", "Sim", "Agora não");
            if (querAssinar)
            {
                PopupCenarios.IsVisible = false; // Fecha o pop-up ao redirecionar
                await Navigation.PushAsync(new PremiumPage());
            }
            return;
        }

        var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
        if (dbService == null) return;

        await dbService.SalvarRegistroAsync(new Models.RegistroFinanceiro
        {
            Data = DateTime.Now,
            TipoMovimentacao = "Ganho",
            Categoria = "Corrida de App",
            Descricao = $"Corrida Aceita ({cenario})",
            Valor = _ganhoBrutoAtual
        });

        await dbService.SalvarRegistroAsync(new Models.RegistroFinanceiro
        {
            Data = DateTime.Now,
            TipoMovimentacao = "Gasto",
            Categoria = "Combustível",
            Descricao = $"Custo da Corrida ({cenario})",
            Valor = gastoCombustivel
        });

        await DisplayAlert("Sucesso! ✅", $"Corrida salva no seu relatório!\n\nGanho: R$ {_ganhoBrutoAtual:F2}\nGasto Combustível: R$ {gastoCombustivel:F2}", "OK");

        PopupCenarios.IsVisible = false;
        BtnLimpar_Clicked(this, EventArgs.Empty);
    }
}