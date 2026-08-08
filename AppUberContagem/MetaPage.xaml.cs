using Microsoft.Maui.Storage;
using System.Globalization;

namespace AppUberContagem;

public partial class MetaPage : ContentPage
{
    public MetaPage()
    {
        InitializeComponent();
    }

    // O OnAppearing garante que a barra atualize toda vez que o usuário abrir a aba
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AtualizarMetaDiaria();
    }

    private async void BtnDefinirMeta_Clicked(object sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("Meta Diária",
            "Qual é o seu objetivo de ganho bruto para hoje?",
            placeholder: "Ex: 250",
            keyboard: Keyboard.Telephone);

        if (!string.IsNullOrWhiteSpace(result))
        {
            if (double.TryParse(result.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double novaMeta))
            {
                Preferences.Default.Set("MetaDiaria", novaMeta);
                await AtualizarMetaDiaria();
            }
            else
            {
                await DisplayAlert("Erro", "Por favor, digite um valor numérico válido.", "OK");
            }
        }
    }

    private async Task AtualizarMetaDiaria()
    {
        // 1. Pega a meta definida (ou 200 como padrão)
        double meta = Preferences.Default.Get("MetaDiaria", 200.0);
        double ganhosHoje = 0;

        // 2. Busca no banco de dados os ganhos de HOJE
        var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
        if (dbService != null)
        {
            // Ajuste o nome "ObterRegistrosAsync" caso no seu código o método chame algo diferente (ex: GetRegistrosAsync)
            var registros = await dbService.ObterRegistrosAsync();

            ganhosHoje = registros
                .Where(r => r.Data.Date == DateTime.Today && r.TipoMovimentacao == "Ganho")
                .Sum(r => r.Valor);
        }

        // 3. Atualiza os textos na tela
        lblGanhosHoje.Text = $"R$ {ganhosHoje:F2}";
        lblValorMeta.Text = $"R$ {meta:F2}";

        // 4. Calcula e anima a barra de progresso (trava em 1.0 = 100% para não dar erro visual)
        double progresso = meta > 0 ? ganhosHoje / meta : 0;
        if (progresso > 1) progresso = 1;

        await barMetaDiaria.ProgressTo(progresso, 500, Easing.CubicOut);
    }
}