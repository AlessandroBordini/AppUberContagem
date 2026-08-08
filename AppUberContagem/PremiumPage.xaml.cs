using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using AppUberContagem.Services;

namespace AppUberContagem;

public partial class PremiumPage : ContentPage
{
    private readonly BillingService _billingService = new BillingService();
    private const string ChavePremium = "usuario_e_premium";

    public PremiumPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ChecarSeJaEPremium();
        await CarregarPrecoProdutoAsync();
    }

    private async Task ChecarSeJaEPremium()
    {
        string salvoNoCelular = await SecureStorage.Default.GetAsync(ChavePremium);
        if (salvoNoCelular == "sim" || Preferences.Default.Get("IsPremium", false))
        {
            Preferences.Default.Set("IsPremium", true);
            AtualizarMenuPrincipal();
            MudarTelaParaPremium();
        }
        else
        {
            bool comprouNoGoogle = await _billingService.IsPremiumPurchasedAsync();
            if (comprouNoGoogle)
            {
                await SecureStorage.Default.SetAsync(ChavePremium, "sim");
                Preferences.Default.Set("IsPremium", true);
                AtualizarMenuPrincipal();
                MudarTelaParaPremium();
            }
        }
    }

    private async Task CarregarPrecoProdutoAsync()
    {
        try { BtnComprar.Text = "Desbloquear Versão Premium"; }
        catch { BtnComprar.Text = "Desbloquear Versão Premium"; }
    }

    private async void OnComprarClicked(object sender, EventArgs e)
    {
        BtnComprar.IsEnabled = false;
        IndicadorCarregamento.IsVisible = true;
        IndicadorCarregamento.IsRunning = true;

        try
        {
            bool compraSucesso = await _billingService.BuyPremiumAsync();
            if (compraSucesso)
            {
                await SecureStorage.Default.SetAsync(ChavePremium, "sim");
                Preferences.Default.Set("IsPremium", true);
                AtualizarMenuPrincipal();
                MudarTelaParaPremium();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Não foi possível concluir a compra: {ex.Message}", "OK");
        }
        finally
        {
            IndicadorCarregamento.IsVisible = false;
            IndicadorCarregamento.IsRunning = false;
            BtnComprar.IsEnabled = true;
        }
    }

    private void AtualizarMenuPrincipal()
    {
        if (Shell.Current is AppShell shell) shell.AtualizarEstadoMenu();
    }

    private void MudarTelaParaPremium()
    {
        BtnComprar.IsVisible = false;
        LblSucesso.IsVisible = true;
    }
}