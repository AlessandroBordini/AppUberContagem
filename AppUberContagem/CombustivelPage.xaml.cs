using Microsoft.Maui.Storage;
using System.Globalization;
using AppUberContagem.Helpers;

namespace AppUberContagem;

public partial class CombustivelPage : ContentPage
{
    private bool _isCalculando = false;

    public CombustivelPage()
    {
        InitializeComponent();

        // Define o estado inicial da UI com base nas preferências salvas
        CarregarConfiguracoesSalvas();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Valida se o usuário é Premium para liberar o Modo Completo / Tanque
        bool isPremium = await PremiumHelper.IsPremiumAsync();
    }

    private void CarregarConfiguracoesSalvas()
    {
        // Deixa o seletor de modo sem seleção forçada (Modo Simples será o padrão visual na tela pois o container dele está visível por padrão e o picker inicia sem índice marcado, ou podemos garantir -1 se preferir)
        pckModoCalculo.SelectedIndex = -1;
        vslModoSimplesContainer.IsVisible = true;
        vslModoCompletoContainer.IsVisible = false;

        bool usaGasolina = Preferences.Default.Get("UsaGasolina", true);
        if (usaGasolina)
        {
            rbGasolina.IsChecked = true;
            txtPrecoLitroSimples.Text = Preferences.Default.Get("PrecoGasolina", "0");
        }
        else
        {
            rbAlcool.IsChecked = true;
            txtPrecoLitroSimples.Text = Preferences.Default.Get("PrecoAlcool", "0");
        }

        AtualizarBarraTanqueVisual();
    }

    private void PckModoCalculo_SelectedIndexChanged(object sender, EventArgs e)
    {
        // Se o índice for -1 (nenhum selecionado), mantém o modo simples padrão
        if (pckModoCalculo.SelectedIndex == -1) return;

        // 0 = Modo Simples, 1 = Modo Completo (Tanque)
        bool isModoCompleto = pckModoCalculo.SelectedIndex == 1;

        if (isModoCompleto)
        {
            Task.Run(async () =>
            {
                bool isPremium = await PremiumHelper.IsPremiumAsync();
                if (!isPremium)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        bool assinar = await DisplayAlert("Recurso Premium ⭐", "O controle completo de tanque e consumo avançado é exclusivo para assinantes. Deseja conhecer o Premium?", "Sim", "Agora não");
                        if (assinar)
                        {
                            await Navigation.PushAsync(new PremiumPage());
                        }
                        pckModoCalculo.SelectedIndex = 0; // Retorna para o simples
                    });
                }
            });
        }

        vslModoSimplesContainer.IsVisible = !isModoCompleto;
        vslModoCompletoContainer.IsVisible = isModoCompleto;
    }

    private void RbCombustivel_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (rbGasolina.IsChecked)
        {
            Preferences.Default.Set("UsaGasolina", true);
            txtPrecoLitroSimples.Text = Preferences.Default.Get("PrecoGasolina", "5.89");
        }
        else if (rbAlcool.IsChecked)
        {
            Preferences.Default.Set("UsaGasolina", false);
            txtPrecoLitroSimples.Text = Preferences.Default.Get("PrecoAlcool", "4.29");
        }
    }

    private async void BtnRegistrar_Clicked(object sender, EventArgs e)
    {
        string textoPreco = txtPrecoLitroSimples.Text;

        if (string.IsNullOrWhiteSpace(textoPreco))
        {
            await DisplayAlert("Atenção", "Informe o preço do combustível.", "OK");
            return;
        }

        if (!double.TryParse(textoPreco.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double precoLitro) || precoLitro <= 0)
        {
            await DisplayAlert("Erro", "Digite um valor válido para o preço.", "OK");
            return;
        }

        bool usaGasolina = rbGasolina.IsChecked;
        Preferences.Default.Set("UsaGasolina", usaGasolina);

        if (usaGasolina)
        {
            Preferences.Default.Set("PrecoGasolina", precoLitro.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            Preferences.Default.Set("PrecoAlcool", precoLitro.ToString(CultureInfo.InvariantCulture));
        }

        await DisplayAlert("Sucesso! ✅", "Preço do combustível atualizado com sucesso!", "OK");
        await Navigation.PopAsync();
    }

    // ==========================================
    // LÓGICA DO MODO COMPLETO / CONTROLE DE TANQUE
    // ==========================================

    private void PckTipoEntradaAbastecimento_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (pckTipoEntradaAbastecimento.SelectedIndex == 1)
        {
            lblCampo1.Text = "Valor Total Pago (R$):";
            txtValor1.Placeholder = "Ex: 100,00";
        }
        else
        {
            lblCampo1.Text = "Quantidade de Litros Colocados:";
            txtValor1.Placeholder = "Ex: 20,5";
        }

        CalcularResumoAbastecimentoParcial();
    }

    private void PckNivelInicial_SelectedIndexChanged(object sender, EventArgs e)
    {
        double percentual = 1.0;
        switch (pckNivelInicial.SelectedIndex)
        {
            case 0: percentual = 1.0; break;     // Cheio (100%)
            case 1: percentual = 0.75; break;    // 3/4 (75%)
            case 2: percentual = 0.50; break;    // Meio (50%)
            case 3: percentual = 0.25; break;    // 1/4 (25%)
            case 4: percentual = 0.10; break;    // Reserva (10%)
        }

        Preferences.Default.Set("NivelTanqueAtualPercentual", percentual.ToString(CultureInfo.InvariantCulture));
        AtualizarBarraTanqueVisual();
    }

    private void CalculadoraAbastecimento_Changed(object sender, TextChangedEventArgs e)
    {
        CalcularResumoAbastecimentoParcial();
    }

    private void CalcularResumoAbastecimentoParcial()
    {
        if (_isCalculando) return;
        _isCalculando = true;

        try
        {
            string val1Str = txtValor1.Text;
            string val2Str = txtValor2.Text;

            if (string.IsNullOrWhiteSpace(val1Str) || string.IsNullOrWhiteSpace(val2Str))
            {
                lblResumoLitros.Text = "Litros adicionados: 0,00 L";
                lblResumoTotal.Text = "Total a pagar / Gasto: R$ 0,00";
                _isCalculando = false;
                return;
            }

            if (!double.TryParse(val1Str.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double v1) ||
                !double.TryParse(val2Str.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double v2))
            {
                _isCalculando = false;
                return;
            }

            double litrosAdicionados = 0;
            double valorTotalGasto = 0;

            int tipoEntrada = pckTipoEntradaAbastecimento.SelectedIndex;
            if (tipoEntrada == 1)
            {
                valorTotalGasto = v1;
                double precoLitro = v2;
                if (precoLitro > 0)
                {
                    litrosAdicionados = valorTotalGasto / precoLitro;
                }
            }
            else
            {
                litrosAdicionados = v1;
                double precoLitro = v2;
                valorTotalGasto = litrosAdicionados * precoLitro;
            }

            lblResumoLitros.Text = $"Litros adicionados: {litrosAdicionados:F2} L";
            lblResumoTotal.Text = $"Total a pagar / Gasto: R$ {valorTotalGasto:F2}";
        }
        finally
        {
            _isCalculando = false;
        }
    }

    private async void BtnRegistrarCompleto_Clicked(object sender, EventArgs e)
    {
        string val1Str = txtValor1.Text;
        string val2Str = txtValor2.Text;

        // Pega a capacidade direto das preferências do veículo agora
        string capacidadeStr = Preferences.Default.Get("CapacidadeTanque", "50");

        if (string.IsNullOrWhiteSpace(val1Str) || string.IsNullOrWhiteSpace(val2Str) || string.IsNullOrWhiteSpace(capacidadeStr))
        {
            await DisplayAlert("Atenção", "Preencha todos os campos do abastecimento e certifique-se de ter cadastrado a capacidade do tanque nas configurações do veículo.", "OK");
            return;
        }

        if (!double.TryParse(val1Str.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double v1) ||
            !double.TryParse(val2Str.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double v2) ||
            !double.TryParse(capacidadeStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double capacidadeTotal))
        {
            await DisplayAlert("Erro", "Valores numéricos inválidos.", "OK");
            return;
        }

        double litrosAdicionados = 0;
        double valorTotalGasto = 0;

        if (pckTipoEntradaAbastecimento.SelectedIndex == 1)
        {
            valorTotalGasto = v1;
            double precoLitro = v2;
            if (precoLitro > 0) litrosAdicionados = valorTotalGasto / precoLitro;
        }
        else
        {
            litrosAdicionados = v1;
            double precoLitro = v2;
            valorTotalGasto = litrosAdicionados * precoLitro;
        }

        var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
        if (dbService != null)
        {
            await dbService.SalvarRegistroAsync(new Models.RegistroFinanceiro
            {
                Data = DateTime.Now,
                TipoMovimentacao = "Gasto",
                Categoria = "Combustível",
                Descricao = $"Abastecimento Completo ({litrosAdicionados:F2} L)",
                Valor = (double)valorTotalGasto
            });
        }

        double percentualAtual = Preferences.Default.Get("NivelTanqueAtualPercentual", 1.0);
        double litrosAtuaisNoTanque = capacidadeTotal * percentualAtual;
        litrosAtuaisNoTanque += litrosAdicionados;

        if (litrosAtuaisNoTanque > capacidadeTotal) litrosAtuaisNoTanque = capacidadeTotal;

        double novoPercentual = capacidadeTotal > 0 ? litrosAtuaisNoTanque / capacidadeTotal : 1.0;
        Preferences.Default.Set("NivelTanqueAtualPercentual", novoPercentual.ToString(CultureInfo.InvariantCulture));

        AtualizarBarraTanqueVisual();

        await DisplayAlert("Sucesso! ✅", $"Abastecimento de R$ {valorTotalGasto:F2} registrado com sucesso no seu fluxo financeiro!", "OK");

        txtValor1.Text = string.Empty;
        txtValor2.Text = string.Empty;
    }

    private void AtualizarBarraTanqueVisual()
    {
        double percentual = Preferences.Default.Get("NivelTanqueAtualPercentual", 1.0);
        if (percentual > 1.0) percentual = 1.0;
        if (percentual < 0) percentual = 0;

        int larguraMaxima = 250;
        double larguraCalculada = larguraMaxima * percentual;

        barNivelTanque.WidthRequest = larguraCalculada;
        lblPercentualTanque.Text = $"{percentual * 100:F0}%";

        if (percentual > 0.5)
        {
            barNivelTanque.BackgroundColor = Colors.Green;
            lblStatusTanqueTexto.Text = "Tanque Bom";
        }
        else if (percentual > 0.2)
        {
            barNivelTanque.BackgroundColor = Colors.Orange;
            lblStatusTanqueTexto.Text = "Atenção (Metade)";
        }
        else
        {
            barNivelTanque.BackgroundColor = Colors.Red;
            lblStatusTanqueTexto.Text = "Reserva!";
        }
    }
}