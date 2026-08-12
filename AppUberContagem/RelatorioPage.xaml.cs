using Microsoft.Maui.Storage;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Fonts;
using System.Globalization;
using System.IO;
using AppUberContagem.Helpers;

using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiColors = Microsoft.Maui.Graphics.Colors;
using Share = Microsoft.Maui.ApplicationModel.DataTransfer.Share;
using ShareFile = Microsoft.Maui.ApplicationModel.DataTransfer.ShareFile;
using ShareFileRequest = Microsoft.Maui.ApplicationModel.DataTransfer.ShareFileRequest;

namespace AppUberContagem;

public partial class RelatorioPage : ContentPage
{
    private bool _isRedirecting = false;
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

        bool isPremium = await PremiumHelper.IsPremiumAsync();
        if (!isPremium)
        {
            _isRedirecting = true;
            bool querAssinar = await DisplayAlert("Função Premium ⭐", "O relatório financeiro detalhado é exclusivo Premium.", "Sim", "Agora não");
            if (querAssinar) await Navigation.PushAsync(new PremiumPage());
            else await Shell.Current.GoToAsync("//MainPage");
            _isRedirecting = false;
            return;
        }
        await CarregarDadosFinanceiros();
    }

    private async void BtnAtualizar_Clicked(object sender, EventArgs e) => await CarregarDadosFinanceiros();

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

    private async void OnDataFiltroChanged(object sender, DateChangedEventArgs e) => await CarregarDadosFinanceiros();

    // --- CADASTRO MANUAL (SEGURO) ---
    private async void BtnNovoLancamento_Clicked(object sender, EventArgs e)
    {
        string tipoEscolha = await DisplayActionSheet("Novo Lançamento", "Cancelar", null, "Ganho (Adição)", "Gasto (Subtração)");
        if (string.IsNullOrEmpty(tipoEscolha) || tipoEscolha == "Cancelar") return;

        string tipoMovimentacao = tipoEscolha.Contains("Ganho") ? "Ganho" : "Gasto";

        string descricao = await DisplayPromptAsyncSeguro("Descrição", "O que é esse lançamento?", "Ex: Troca de óleo");
        if (string.IsNullOrWhiteSpace(descricao)) return;

        string valorStr = await DisplayPromptAsyncSeguro("Valor", "Digite o valor em R$:", "0.00", Keyboard.Telephone);
        if (string.IsNullOrWhiteSpace(valorStr)) return;

        string dataStr = await DisplayPromptAsyncSeguro("Data", "Data (DD/MM/AAAA):", DateTime.Today.ToString("dd/MM/yyyy"));
        if (string.IsNullOrWhiteSpace(dataStr)) return;

        if (!DateTime.TryParseExact(dataStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataLancamento))
        {
            await DisplayAlert("Erro", "Formato de data inválido.", "OK");
            return;
        }

        if (double.TryParse(valorStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor))
        {
            var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
            await dbService.SalvarRegistroAsync(new Models.RegistroFinanceiro { TipoMovimentacao = tipoMovimentacao, Categoria = "Manual", Descricao = descricao, Valor = valor, Data = dataLancamento });
            await CarregarDadosFinanceiros();
        }
    }

    // --- EXPORTAR PDF (SEGURO) ---
    private async void OnExportarPdfClicked(object sender, EventArgs e)
    {
        try
        {
            string dataInicioPrompt = await DisplayPromptAsyncSeguro("Relatório PDF", "Data Inicial (DD/MM/AAAA):", DateTime.Today.AddDays(-30).ToString("dd/MM/yyyy"));
            if (string.IsNullOrWhiteSpace(dataInicioPrompt)) return;

            string dataFimPrompt = await DisplayPromptAsyncSeguro("Relatório PDF", "Data Final (DD/MM/AAAA):", DateTime.Today.ToString("dd/MM/yyyy"));
            if (string.IsNullOrWhiteSpace(dataFimPrompt)) return;

            if (!DateTime.TryParseExact(dataInicioPrompt, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataInicioExport) ||
                !DateTime.TryParseExact(dataFimPrompt, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataFimExport))
            {
                await DisplayAlert("Erro", "Formato de data inválido.", "OK");
                return;
            }

            DateTime dataFimAjustada = dataFimExport.Date.AddDays(1).AddSeconds(-1);

            var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
            if (dbService == null) return;

            var todosRegistros = await dbService.ObterRegistrosAsync();
            var listaPdf = todosRegistros?
                .Where(r => r.Data >= dataInicioExport.Date && r.Data <= dataFimAjustada)
                .OrderBy(r => r.Data)
                .ToList();

            if (listaPdf == null || !listaPdf.Any())
            {
                await DisplayAlert("Aviso", "Nenhum registro encontrado no período.", "OK");
                return;
            }

            double ganhosPdf = listaPdf.Where(r => r.TipoMovimentacao == "Ganho").Sum(r => r.Valor);
            double gastosPdf = listaPdf.Where(r => r.TipoMovimentacao == "Gasto").Sum(r => r.Valor);
            double lucroPdf = ganhosPdf - gastosPdf;

            if (!_fontResolverConfigurado)
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("OpenSans-Regular.ttf");
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                GlobalFontSettings.FontResolver = new SimpleFontResolver(memoryStream.ToArray());
                _fontResolverConfigurado = true;
            }

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
            gfx.DrawString($"Período: {dataInicioPrompt} até {dataFimPrompt}", fontSub, XBrushes.Gray, new XPoint(40, yPoint));
            yPoint += 35;

            gfx.DrawString("Resumo", fontBold, XBrushes.Black, new XPoint(40, yPoint));
            yPoint += 20;

            gfx.DrawString($"Ganhos: R$ {ganhosPdf:F2}", fontNormal, XBrushes.Green, new XPoint(50, yPoint));
            yPoint += 18;
            gfx.DrawString($"Gastos: R$ {gastosPdf:F2}", fontNormal, XBrushes.Red, new XPoint(50, yPoint));
            yPoint += 18;
            gfx.DrawString($"Líquido: R$ {lucroPdf:F2}", fontBold, XBrushes.Blue, new XPoint(50, yPoint));
            yPoint += 30;

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
            await DisplayAlert("Erro", $"Erro ao gerar PDF: {ex.Message}", "OK");
        }
    }

    // --- POPUP SEGURO CORRIGIDO (AGORA COM BOTÃO DE SAIR) ---
    private async Task<string?> DisplayPromptAsyncSeguro(string titulo, string mensagem, string valorInicial = "", Keyboard? teclado = null)
    {
        var entry = new Entry
        {
            Placeholder = "Digite aqui...",
            Text = valorInicial,
            Keyboard = teclado ?? Keyboard.Default,
            HeightRequest = 50,
            BackgroundColor = Color.FromArgb("#F1F3F5")
        };

        var tcs = new TaskCompletionSource<string?>();

        // Botão de Confirmar Original
        var btnSalvar = new Button
        {
            Text = "Confirmar",
            BackgroundColor = Color.FromArgb("#007BFF"),
            TextColor = Colors.White,
            HeightRequest = 50,
            CornerRadius = 8
        };
        btnSalvar.Clicked += async (s, args) =>
        {
            string res = entry.Text;
            await Navigation.PopModalAsync();
            tcs.SetResult(res);
        };

        // Novo Botão de Cancelar (Fica ao lado do Confirmar)
        var btnCancelar = new Button
        {
            Text = "Cancelar",
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.Red,
            HeightRequest = 50,
            CornerRadius = 8
        };
        btnCancelar.Clicked += async (s, args) =>
        {
            await Navigation.PopModalAsync();
            tcs.SetResult(null); // Retorna nulo para indicar o cancelamento
        };

        // Agrupando os botões de Cancelar e Confirmar lado a lado
        var gridBotoes = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Star } },
            ColumnSpacing = 10
        };
        gridBotoes.Add(btnCancelar, 0, 0);
        gridBotoes.Add(btnSalvar, 1, 0);

        // Novo botão de Fechar (Um "X" no canto superior direito)
        var btnFecharCanto = new Button
        {
            Text = "❌",
            BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 10, 10, 0),
            WidthRequest = 45,
            HeightRequest = 45
        };
        btnFecharCanto.Clicked += async (s, args) =>
        {
            await Navigation.PopModalAsync();
            tcs.SetResult(null); // Retorna nulo para indicar o cancelamento
        };

        // Layout Principal do Popup
        var layoutPrincipal = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 20,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = titulo, FontSize = 22, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center },
                new Label { Text = mensagem, TextColor = Colors.Gray, FontSize = 14 },
                entry,
                gridBotoes // Adiciona os dois botões (Cancelar e Confirmar)
            }
        };

        var dialogPage = new ContentPage
        {
            Title = titulo,
            BackgroundColor = Colors.White,
            Content = new Grid
            {
                Children =
                {
                    layoutPrincipal, // O conteúdo no centro
                    btnFecharCanto   // O "X" no canto
                }
            }
        };

        await Navigation.PushModalAsync(dialogPage);
        return await tcs.Task;
    }

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

        _totalGanhos = _listaFiltrada.Where(r => r.TipoMovimentacao == "Ganho").Sum(r => r.Valor);
        _totalGastos = _listaFiltrada.Where(r => r.TipoMovimentacao == "Gasto").Sum(r => r.Valor);
        _lucroLiquido = _totalGanhos - _totalGastos;

        lblTotalGanhos.Text = $"R$ {_totalGanhos:F2}";
        lblTotalGastos.Text = $"R$ {_totalGastos:F2}";
        lblLucroLiquido.Text = $"R$ {_lucroLiquido:F2}";
        lblLucroLiquido.TextColor = _lucroLiquido >= 0 ? MauiColors.Blue : MauiColors.Red;
        cvRegistros.ItemsSource = _listaFiltrada;
    }

    private async void BtnDeletar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int id)
        {
            if (await DisplayAlert("Excluir", "Apagar este registro?", "Sim", "Não"))
            {
                var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
                await dbService.DeletarRegistroAsync(id);
                await CarregarDadosFinanceiros();
            }
        }
    }
}