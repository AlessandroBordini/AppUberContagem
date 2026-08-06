using Microsoft.Maui.Storage;

namespace AppUberContagem;

public partial class RelatoriosPage : ContentPage
{
    private bool _isRedirecting = false;

    public RelatoriosPage()
    {
        InitializeComponent();
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

    private async Task CarregarDadosFinanceiros()
    {
        var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
        if (dbService == null) return;

        // Puxa todos os registros salvos no SQLite
        var listaRegistros = await dbService.ObterRegistrosAsync();

        if (listaRegistros == null) return;

        // Calcula os totais
        double totalGanhos = listaRegistros
            .Where(r => r.TipoMovimentacao == "Ganho")
            .Sum(r => r.Valor);

        double totalGastos = listaRegistros
            .Where(r => r.TipoMovimentacao == "Gasto")
            .Sum(r => r.Valor);

        double lucroLiquido = totalGanhos - totalGastos;

        // Atualiza a interface
        lblTotalGanhos.Text = $"R$ {totalGanhos:F2}";
        lblTotalGastos.Text = $"R$ {totalGastos:F2}";
        lblLucroLiquido.Text = $"R$ {lucroLiquido:F2}";
        lblLucroLiquido.TextColor = lucroLiquido >= 0 ? Colors.Blue : Colors.Red;

        // Preenche a lista na tela (ordenada da mais recente para a mais antiga)
        cvRegistros.ItemsSource = listaRegistros.OrderByDescending(r => r.Data).ToList();
    }
}