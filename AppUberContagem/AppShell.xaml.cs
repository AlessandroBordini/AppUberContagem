using AppUberContagem.Helpers;
using Microsoft.Maui.Storage;

namespace AppUberContagem;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = AtualizarEstadoMenu();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent != null)
        {
            _ = AtualizarEstadoMenu();
        }
    }

    // Alterado para 'async Task' para funcionar corretamente com o '_'
    public async Task AtualizarEstadoMenu()
    {
        bool isPremium = await PremiumHelper.IsPremiumAsync();

        if (isPremium)
        {
            itemRelatorio.Title = "📊 Relatório Financeiro";
            itemMetas.Title = "🎯 Metas";
        }
        else
        {
            itemRelatorio.Title = "🔒 Relatório Financeiro (PRO)";
            itemMetas.Title = "🔒 Metas (PRO)";
        }
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        string target = args.Target.Location.OriginalString;

        bool isPremium = Preferences.Default.Get("IsPremium", false);

        if ((target.Contains("RelatoriosPage") || target.Contains("MetaPage")) && !isPremium)
        {
            args.Cancel();

            string recurso = target.Contains("RelatoriosPage") ? "o relatório financeiro" : "o acompanhamento de metas";

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool querAssinar = await Current.DisplayAlert(
                    "Função Premium ⭐",
                    $"O {recurso} é exclusivo para assinantes Premium. Deseja desbloquear agora?",
                    "Sim",
                    "Agora não"
                );

                if (querAssinar)
                {
                    await Current.Navigation.PushAsync(new PremiumPage());
                }
            });
        }
    }
}