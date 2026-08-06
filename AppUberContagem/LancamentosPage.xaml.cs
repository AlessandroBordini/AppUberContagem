using Microsoft.Maui.Storage;
using System.Globalization;

namespace AppUberContagem;

public partial class LancamentosPage : ContentPage
{
    public LancamentosPage()
    {
        InitializeComponent();
        dtpData.Date = DateTime.Now;
    }

    private async void BtnSalvar_Clicked(object sender, EventArgs e)
    {
        bool isPremium = Preferences.Default.Get("IsPremium", false);
        if (!isPremium)
        {
            bool querAssinar = await DisplayAlert("Função Premium ⭐", "O registro financeiro manual é exclusivo para assinantes. Deseja conhecer?", "Sim", "Agora não");
            if (querAssinar)
            {
                await Navigation.PushAsync(new PremiumPage());
            }
            return;
        }

        if (pckTipo.SelectedIndex == -1 || pckCategoria.SelectedIndex == -1 || string.IsNullOrWhiteSpace(txtValor.Text))
        {
            await DisplayAlert("Atenção", "Por favor, preencha o Tipo, a Categoria e o Valor.", "OK");
            return;
        }

        if (!double.TryParse(txtValor.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double valorReal))
        {
            await DisplayAlert("Erro", "Digite um valor numérico válido.", "OK");
            return;
        }

        var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
        if (dbService == null) return;

        var novoRegistro = new Models.RegistroFinanceiro
        {
            Data = dtpData.Date ?? DateTime.Now, // <--- Altere aqui
            TipoMovimentacao = pckTipo.SelectedItem.ToString(),
            Categoria = pckCategoria.SelectedItem.ToString(),
            Descricao = txtDescricao.Text ?? "Sem descrição",
            Valor = valorReal
        };

        await dbService.SalvarRegistroAsync(novoRegistro);

        await DisplayAlert("Sucesso ✅", "Lançamento salvo com sucesso no seu fechamento!", "OK");

        txtDescricao.Text = string.Empty;
        txtValor.Text = string.Empty;
        pckTipo.SelectedIndex = -1;
        pckCategoria.SelectedIndex = -1;
        dtpData.Date = DateTime.Now;
    }
}