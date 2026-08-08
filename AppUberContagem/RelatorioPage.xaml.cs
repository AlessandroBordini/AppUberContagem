using Microsoft.Maui.Storage;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Fonts;
using System.Globalization;
using System.IO;

// Apelidos para evitar conflito entre as cores do MAUI e do PDF
using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiColors = Microsoft.Maui.Graphics.Colors;
using Share = Microsoft.Maui.ApplicationModel.DataTransfer.Share;
using ShareFile = Microsoft.Maui.ApplicationModel.DataTransfer.ShareFile;
using ShareFileRequest = Microsoft.Maui.ApplicationModel.DataTransfer.ShareFileRequest;

namespace AppUberContagem;

public partial class RelatorioPage : ContentPage
{
    private bool _isRedirecting = false;

    // VARIÁVEL NOVA: Controla se a fonte já foi carregada sem bugar o Android
    private static bool _fontResolverConfigurado = false;

    private List<Models.RegistroFinanceiro> _listaFiltrada = new();
    private double _totalGanhos = 0;
    private double _totalGastos = 0;
    private double _lucroLiquido = 0;

    public RelatorioPage()
    {
        InitializeComponent();

        dtpInicio.Date = DateTime.Today;
        dtpFim.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isRedirecting) return;

        bool isPremium = Preferences.Default.Get("IsPremium", false);

        if (!isPremium)
        {
            _isRedirecting = true;

            bool querAssinar = await DisplayAlert(
                "Função Premium ⭐",
                "O relatório financeiro detalhado é uma exclusividade para assinantes Premium. Deseja conhecer os benefícios?",
                "Sim",
                "Agora não"
            );

            if (querAssinar)
            {
                await Navigation.PushAsync(new PremiumPage());
            }
            else
            {
                await Shell.Current.GoToAsync("//MainPage");
            }

            _isRedirecting = false;
            return;
        }

        await CarregarDadosFinanceiros();
    }

    private async void BtnAtualizar_Clicked(object sender, EventArgs e)
    {
        bool isPremium = Preferences.Default.Get("IsPremium", false);
        if (!isPremium) return;

        await CarregarDadosFinanceiros();
    }

    // --- FILTROS RÁPIDOS DE PERÍODO (TELA) ---
    private void DestacarBotaoFiltro(Button botaoAtivo)
    {
        btnHoje.BackgroundColor = MauiColor.FromArgb("#6C757D");
        btnSemana.BackgroundColor = MauiColor.FromArgb("#6C757D");
        btnMes.BackgroundColor = MauiColor.FromArgb("#6C757D");

        botaoAtivo.BackgroundColor = MauiColor.FromArgb("#007BFF");
    }

    private void BtnFiltroHoje_Clicked(object sender, EventArgs e)
    {
        dtpInicio.Date = DateTime.Today;
        dtpFim.Date = DateTime.Today;
        DestacarBotaoFiltro(btnHoje);
    }

    private void BtnFiltroSemana_Clicked(object sender, EventArgs e)
    {
        var hoje = DateTime.Today;
        int diff = (int)hoje.DayOfWeek;
        dtpInicio.Date = hoje.AddDays(-diff);
        dtpFim.Date = hoje;
        DestacarBotaoFiltro(btnSemana);
    }

    private void BtnFiltroMes_Clicked(object sender, EventArgs e)
    {
        var hoje = DateTime.Today;
        dtpInicio.Date = new DateTime(hoje.Year, hoje.Month, 1);
        dtpFim.Date = hoje;
        DestacarBotaoFiltro(btnMes);
    }

    private async void OnDataFiltroChanged(object sender, DateChangedEventArgs e)
    {
        await CarregarDadosFinanceiros();
    }

    // --- CADASTRO MANUAL DE LANÇAMENTO ---
    private async void BtnNovoLancamento_Clicked(object sender, EventArgs e)
    {
        string tipoEscolha = await DisplayActionSheet("Novo Lançamento", "Cancelar", null, "Ganho (Adição)", "Gasto (Subtração)");
        if (string.IsNullOrEmpty(tipoEscolha) || tipoEscolha == "Cancelar") return;

        string tipoMovimentacao = tipoEscolha.Contains("Ganho") ? "Ganho" : "Gasto";

        string descricao = await DisplayPromptAsync("Descrição", "O que é esse lançamento?", placeholder: "Ex: Troca de óleo, Corrida por fora...", accept: "Avançar", cancel: "Cancelar");
        if (string.IsNullOrWhiteSpace(descricao)) return;

        string valorStr = await DisplayPromptAsync("Valor", "Digite o valor em R$:", placeholder: "Ex: 50,00", keyboard: Keyboard.Telephone, accept: "Avançar", cancel: "Cancelar");
        if (string.IsNullOrWhiteSpace(valorStr)) return;

        string dataStr = await DisplayPromptAsync("Data", "Data do lançamento (DD/MM/AAAA):", initialValue: DateTime.Today.ToString("dd/MM/yyyy"), accept: "Salvar", cancel: "Cancelar");
        if (string.IsNullOrWhiteSpace(dataStr)) return;

        if (!DateTime.TryParseExact(dataStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataLancamento))
        {
            await DisplayAlert("Erro", "Formato de data inválido. Use o padrão DD/MM/AAAA.", "OK");
            return;
        }

        if (double.TryParse(valorStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor))
        {
            var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
            if (dbService == null) return;

            var novoRegistro = new Models.RegistroFinanceiro
            {
                TipoMovimentacao = tipoMovimentacao,
                Categoria = "Manual",
                Descricao = descricao,
                Valor = valor,
                Data = dataLancamento
            };

            await dbService.SalvarRegistroAsync(novoRegistro);
            await CarregarDadosFinanceiros();
        }
        else
        {
            await DisplayAlert("Erro", "Valor numérico inválido.", "OK");
        }
    }

    // --- DELETAR LANÇAMENTO ERRADO ---
    private async void BtnDeletar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int id)
        {
            bool confirmar = await DisplayAlert("Excluir Lançamento", "Deseja realmente apagar este registro?", "Sim", "Não");
            if (confirmar)
            {
                var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
                if (dbService != null)
                {
                    await dbService.DeletarRegistroAsync(id);
                    await CarregarDadosFinanceiros();
                }
            }
        }
    }

    // --- EXPORTAR RELATÓRIO EM PDF COM DATAS ESPECÍFICAS ---
    private async void OnExportarPdfClicked(object sender, EventArgs e)
    {
        try
        {
            // 1. Pergunta a Data Inicial (Já sugere o dia 1º do mês atual)
            string dataInicioPrompt = await DisplayPromptAsync("Relatório PDF", "Data Inicial (DD/MM/AAAA):", initialValue: new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("dd/MM/yyyy"), accept: "Avançar", cancel: "Cancelar");
            if (string.IsNullOrWhiteSpace(dataInicioPrompt)) return;

            if (!DateTime.TryParseExact(dataInicioPrompt, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataInicioExport))
            {
                await DisplayAlert("Erro", "Formato de data inicial inválido.", "OK");
                return;
            }

            // 2. Pergunta a Data Final (Já sugere a data de hoje)
            string dataFimPrompt = await DisplayPromptAsync("Relatório PDF", "Data Final (DD/MM/AAAA):", initialValue: DateTime.Today.ToString("dd/MM/yyyy"), accept: "Gerar PDF", cancel: "Cancelar");
            if (string.IsNullOrWhiteSpace(dataFimPrompt)) return;

            if (!DateTime.TryParseExact(dataFimPrompt, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataFimExport))
            {
                await DisplayAlert("Erro", "Formato de data final inválido.", "OK");
                return;
            }

            // Ajusta a data final para até 23:59:59 do dia escolhido
            DateTime dataFimAjustada = dataFimExport.Date.AddDays(1).AddSeconds(-1);

            // 3. Busca no banco de dados APENAS os registros desse período escolhido
            var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
            if (dbService == null) return;

            var todosRegistros = await dbService.ObterRegistrosAsync();
            var listaPdf = todosRegistros?
                .Where(r => r.Data >= dataInicioExport.Date && r.Data <= dataFimAjustada)
                .OrderBy(r => r.Data) // Coloca em ordem cronológica de data
                .ToList();

            if (listaPdf == null || !listaPdf.Any())
            {
                await DisplayAlert("Aviso", $"Não existem corridas ou registros salvos entre {dataInicioPrompt} e {dataFimPrompt}.", "OK");
                return;
            }

            // 4. Calcula os totais apenas para as datas que vão para o PDF
            double ganhosPdf = listaPdf.Where(r => r.TipoMovimentacao == "Ganho").Sum(r => r.Valor);
            double gastosPdf = listaPdf.Where(r => r.TipoMovimentacao == "Gasto").Sum(r => r.Valor);
            double lucroPdf = ganhosPdf - gastosPdf;

            // Prepara a Fonte do PDF (Bypass de erro no Android)
            if (!_fontResolverConfigurado)
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("OpenSans-Regular.ttf");
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                GlobalFontSettings.FontResolver = new SimpleFontResolver(memoryStream.ToArray());
                _fontResolverConfigurado = true;
            }

            // Nome e Criação do Arquivo PDF
            string nomeArquivo = $"Relatorio_{dataInicioExport:dd-MM}_{dataFimExport:dd-MM}.pdf";
            string caminhoArquivo = Path.Combine(FileSystem.CacheDirectory, nomeArquivo);

            PdfDocument document = new PdfDocument();
            document.Info.Title = "Relatório Financeiro";

            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            XFont fontTitulo = new XFont("Arial", 16, XFontStyle.Bold);
            XFont fontSub = new XFont("Arial", 10, XFontStyle.Regular);
            XFont fontBold = new XFont("Arial", 10, XFontStyle.Bold);
            XFont fontNormal = new XFont("Arial", 10, XFontStyle.Regular);

            double yPoint = 40;

            gfx.DrawString("AppUberContagem", fontTitulo, XBrushes.DarkGreen, new XPoint(40, yPoint));
            yPoint += 20;
            gfx.DrawString($"Período impresso: {dataInicioPrompt} até {dataFimPrompt}", fontSub, XBrushes.Gray, new XPoint(40, yPoint));
            yPoint += 35;

            // Imprime os Valores Resumidos do Período Específico
            gfx.DrawString("Resumo do Período", fontBold, XBrushes.Black, new XPoint(40, yPoint));
            yPoint += 20;

            gfx.DrawString($"Total de Ganhos: R$ {ganhosPdf:F2}", fontNormal, XBrushes.Green, new XPoint(50, yPoint));
            yPoint += 18;
            gfx.DrawString($"Total de Gastos: R$ {gastosPdf:F2}", fontNormal, XBrushes.Red, new XPoint(50, yPoint));
            yPoint += 18;
            gfx.DrawString($"Lucro Líquido: R$ {lucroPdf:F2}", fontBold, XBrushes.Blue, new XPoint(50, yPoint));
            yPoint += 30;

            gfx.DrawString("Detalhamento dos Lançamentos", fontBold, XBrushes.Black, new XPoint(40, yPoint));
            yPoint += 20;

            gfx.DrawString("Data", fontBold, XBrushes.Black, new XPoint(40, yPoint));
            gfx.DrawString("Tipo", fontBold, XBrushes.Black, new XPoint(120, yPoint));
            gfx.DrawString("Descrição", fontBold, XBrushes.Black, new XPoint(200, yPoint));
            gfx.DrawString("Valor", fontBold, XBrushes.Black, new XPoint(400, yPoint));
            yPoint += 15;

            gfx.DrawLine(XPens.Gray, 40, yPoint, 550, yPoint);
            yPoint += 10;

            // Imprime a Tabela com os dados filtrados
            foreach (var reg in listaPdf)
            {
                if (yPoint > page.Height - 50)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPoint = 40;
                }

                XBrush corTexto = reg.TipoMovimentacao == "Ganho" ? XBrushes.Green : XBrushes.Red;

                gfx.DrawString(reg.Data.ToString("dd/MM/yyyy"), fontNormal, XBrushes.Black, new XPoint(40, yPoint));
                gfx.DrawString(reg.TipoMovimentacao, fontNormal, corTexto, new XPoint(120, yPoint));
                gfx.DrawString(reg.Descricao, fontNormal, XBrushes.Black, new XPoint(200, yPoint));
                gfx.DrawString($"R$ {reg.Valor:F2}", fontNormal, corTexto, new XPoint(400, yPoint));

                yPoint += 20;
            }

            document.Save(caminhoArquivo);
            document.Close();

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Compartilhar Relatório PDF",
                File = new ShareFile(caminhoArquivo)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Não foi possível gerar o PDF: {ex.Message}", "OK");
        }
    }

    // --- CARREGAMENTO E FILTRAGEM DE DADOS NA TELA PRINCIPAL ---
    private async Task CarregarDadosFinanceiros()
    {
        var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
        if (dbService == null) return;

        var listaRegistros = await dbService.ObterRegistrosAsync();
        if (listaRegistros == null) return;

        DateTime dataInicio = dtpInicio.Date.HasValue ? dtpInicio.Date.Value.Date : DateTime.Today;
        DateTime dataFim = dtpFim.Date.HasValue ? dtpFim.Date.Value.Date.AddDays(1).AddSeconds(-1) : DateTime.Today;

        _listaFiltrada = listaRegistros
            .Where(r => r.Data.Date >= dataInicio.Date && r.Data.Date <= dataFim.Date)
            .OrderByDescending(r => r.Data)
            .ToList();

        _totalGanhos = _listaFiltrada
            .Where(r => r.TipoMovimentacao == "Ganho")
            .Sum(r => r.Valor);

        _totalGastos = _listaFiltrada
            .Where(r => r.TipoMovimentacao == "Gasto")
            .Sum(r => r.Valor);

        _lucroLiquido = _totalGanhos - _totalGastos;

        lblTotalGanhos.Text = $"R$ {_totalGanhos:F2}";
        lblTotalGastos.Text = $"R$ {_totalGastos:F2}";
        lblLucroLiquido.Text = $"R$ {_lucroLiquido:F2}";
        lblLucroLiquido.TextColor = _lucroLiquido >= 0 ? MauiColors.Blue : MauiColors.Red;

        cvRegistros.ItemsSource = _listaFiltrada;
    }
}