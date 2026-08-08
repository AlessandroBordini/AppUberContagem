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
        AtualizarEstadoMenu();
    }

    // Garante que o menu atualiza assim que o AppShell volta a aparecer em foco
    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent != null)
        {
            AtualizarEstadoMenu();
        }
    }

    public void AtualizarEstadoMenu()
    {
        bool isPremium = Preferences.Default.Get("IsPremium", false);

        if (isPremium)
        {
            // Se for assinante Premium, remove o cadeado e exibe ícones visuais limpos
            itemRelatorio.Title = "📊 Relatório Financeiro";
            itemMetas.Title = "🎯 Metas";
        }
        else
        {
            // Se for versão Free, exibe o cadeado e o selo PRO em ambos
            itemRelatorio.Title = "🔒 Relatório Financeiro (PRO)";
            itemMetas.Title = "🔒 Metas (PRO)";
        }
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        // Se o usuário tentar navegar para Relatórios ou Metas sem ser Premium
        string target = args.Target.Location.OriginalString;

        if (target.Contains("RelatoriosPage") || target.Contains("MetaPage"))
        {
            bool isPremium = Preferences.Default.Get("IsPremium", false);

            if (!isPremium)
            {
                // Cancela a navegação
                args.Cancel();

                // Define o nome amigável do recurso bloqueado
                string recurso = target.Contains("RelatoriosPage") ? "o relatório financeiro" : "o acompanhamento de metas";

                // Exibe o aviso convidativo
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
}