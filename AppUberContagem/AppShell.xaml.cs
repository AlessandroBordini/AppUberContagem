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

    public void AtualizarEstadoMenu()
    {
        bool isPremium = Preferences.Default.Get("IsPremium", false);

        if (isPremium)
        {
            // Se for assinante Premium, remove o aviso de PRO do menu e coloca o ícone normal
            itemRelatorios.Title = "Relatório Financeiro";
            itemRelatorios.Icon = "relatorio_normal.png"; // Certifique-se de ter essa imagem ou remova a linha se não usar ícone normal
        }
        else
        {
            // Se for versão Free, exibe o cadeado e o selo PRO no título do menu
            itemRelatorios.Title = "🔒 Relatório Financeiro (PRO)";
            itemRelatorios.Icon = "lock_icon.png";
        }
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        // Se o usuário tentar navegar para a página de relatórios sem ser Premium
        if (args.Target.Location.OriginalString.Contains("RelatoriosPage"))
        {
            bool isPremium = Preferences.Default.Get("IsPremium", false);

            if (!isPremium)
            {
                // Cancela a navegação para impedir que abra a tela bloqueada
                args.Cancel();

                // Exibe o aviso convidativo para assinar o Premium
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    bool querAssinar = await Current.DisplayAlert(
                        "Função Premium ⭐",
                        "O relatório financeiro é exclusivo para assinantes Premium. Deseja desbloquear agora?",
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