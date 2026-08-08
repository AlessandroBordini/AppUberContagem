using Microsoft.Maui.Storage;
using System.Globalization;
using System.Text.Json;
using AppUberContagem.Helpers;

namespace AppUberContagem;

public partial class MetaPage : ContentPage
{
    private bool _isRedirecting = false;
    private const string ChaveListaMetas = "lista_metas_personalizadas_v1";

    public MetaPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isRedirecting) return;

        // Validação de segurança Premium centralizada
        bool isPremium = await PremiumHelper.IsPremiumAsync();

        if (!isPremium)
        {
            _isRedirecting = true;

            bool querAssinar = await DisplayAlert(
                "Função Premium ⭐",
                "O painel de metas é uma exclusividade para assinantes Premium. Deseja conhecer os benefícios?",
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

        await CarregarMetasAsync();
    }

    // --- CRIAR NOVA META ---
    private async void BtnCriarMeta_Clicked(object sender, EventArgs e)
    {
        bool isPremium = await PremiumHelper.IsPremiumAsync();
        if (!isPremium) return;

        string nomeMeta = await DisplayPromptAsyncSeguro("Nova Meta", "Digite o nome da meta (ex: Meta do Dia, Faturamento...):", "");
        if (string.IsNullOrWhiteSpace(nomeMeta)) return;

        string valorStr = await DisplayPromptAsyncSeguro("Valor Alvo", "Digite o valor em R$ para esta meta:", "", Keyboard.Telephone);
        if (string.IsNullOrWhiteSpace(valorStr)) return;

        if (double.TryParse(valorStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double valorAlvo))
        {
            var metas = ObterMetasSalvas();
            metas.Add(new MetaModel
            {
                Id = Guid.NewGuid().ToString(),
                Nome = nomeMeta,
                ValorAlvo = valorAlvo
            });

            SalvarMetas(metas);
            await CarregarMetasAsync();
        }
        else
        {
            await DisplayAlert("Erro", "Valor numérico inválido.", "OK");
        }
    }

    // --- EDITAR META EXISTENTE ---
    private async void BtnEditar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string id)
        {
            var metas = ObterMetasSalvas();
            var metaParaEditar = metas.FirstOrDefault(m => m.Id == id);
            if (metaParaEditar == null) return;

            string novoNome = await DisplayPromptAsyncSeguro("Editar Meta", "Altere o nome da meta:", metaParaEditar.Nome);
            if (string.IsNullOrWhiteSpace(novoNome)) return;

            string novoValorStr = await DisplayPromptAsyncSeguro("Editar Valor", "Altere o valor alvo em R$:", metaParaEditar.ValorAlvo.ToString("F2", CultureInfo.InvariantCulture), Keyboard.Telephone);
            if (string.IsNullOrWhiteSpace(novoValorStr)) return;

            if (double.TryParse(novoValorStr.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double novoValor))
            {
                metaParaEditar.Nome = novoNome;
                metaParaEditar.ValorAlvo = novoValor;

                SalvarMetas(metas);
                await CarregarMetasAsync();
            }
            else
            {
                await DisplayAlert("Erro", "Valor numérico inválido.", "OK");
            }
        }
    }

    // --- DELETAR META ---
    private async void BtnDeletar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string id)
        {
            bool confirmar = await DisplayAlert("Excluir Meta", "Deseja realmente apagar esta meta?", "Sim", "Não");
            if (confirmar)
            {
                var metas = ObterMetasSalvas();
                var metaRemover = metas.FirstOrDefault(m => m.Id == id);
                if (metaRemover != null)
                {
                    metas.Remove(metaRemover);
                    SalvarMetas(metas);
                    await CarregarMetasAsync();
                }
            }
        }
    }

    // --- POPUP SEGURO (Substituto do DisplayPromptAsync para evitar travamento no emulador) ---
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

        var dialogPage = new ContentPage
        {
            Title = titulo,
            BackgroundColor = Colors.White,
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 20,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = titulo, FontSize = 22, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = mensagem, TextColor = Colors.Gray, FontSize = 14 },
                    entry,
                    new Button
                    {
                        Text = "Salvar",
                        BackgroundColor = Color.FromArgb("#007BFF"),
                        TextColor = Colors.White,
                        CornerRadius = 8,
                        HeightRequest = 50
                    }
                }
            }
        };

        var btnSalvar = (dialogPage.Content as VerticalStackLayout)?.Children.Last() as Button;
        if (btnSalvar != null)
        {
            btnSalvar.Clicked += async (s, args) =>
            {
                string resultado = entry.Text;
                await Navigation.PopModalAsync();
                tcs.SetResult(resultado);
            };
        }

        await Navigation.PushModalAsync(dialogPage);
        return await tcs.Task;
    }

    // --- CARREGAR E CALCULAR PROGRESSO ---
    private async Task CarregarMetasAsync()
    {
        double ganhosHoje = 0;

        var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
        if (dbService != null)
        {
            var registros = await dbService.ObterRegistrosAsync();
            if (registros != null)
            {
                ganhosHoje = registros
                    .Where(r => r.Data.Date == DateTime.Today && r.TipoMovimentacao == "Ganho")
                    .Sum(r => r.Valor);
            }
        }

        var metasSalvas = ObterMetasSalvas();

        // Se o usuário ainda não tiver nenhuma meta cadastrada, cria uma padrão inicial
        if (!metasSalvas.Any())
        {
            metasSalvas.Add(new MetaModel { Id = Guid.NewGuid().ToString(), Nome = "Meta do Dia (Bruto)", ValorAlvo = 200.0 });
            SalvarMetas(metasSalvas);
        }

        var listaViewModel = metasSalvas.Select(m => new MetaViewModel
        {
            Id = m.Id,
            Nome = m.Nome,
            ValorAlvo = m.ValorAlvo,
            GanhosAtuais = ganhosHoje,
            Progresso = m.ValorAlvo > 0 ? Math.Min(ganhosHoje / m.ValorAlvo, 1.0) : 0
        }).ToList();

        cvMetas.ItemsSource = listaViewModel;
    }

    private List<MetaModel> ObterMetasSalvas()
    {
        string json = Preferences.Default.Get(ChaveListaMetas, string.Empty);
        if (string.IsNullOrWhiteSpace(json)) return new List<MetaModel>();

        try
        {
            return JsonSerializer.Deserialize<List<MetaModel>>(json) ?? new List<MetaModel>();
        }
        catch
        {
            return new List<MetaModel>();
        }
    }

    private void SalvarMetas(List<MetaModel> metas)
    {
        string json = JsonSerializer.Serialize(metas);
        Preferences.Default.Set(ChaveListaMetas, json);
    }
}

// Modelos auxiliares locais para gerenciar os dados
public class MetaModel
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public double ValorAlvo { get; set; }
}

public class MetaViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public double ValorAlvo { get; set; }
    public double GanhosAtuais { get; set; }
    public double Progresso { get; set; }
    public string GanhosFormatados => $"R$ {GanhosAtuais:F2}";
    public string AlvoFormatado => $"R$ {ValorAlvo:F2}";
}