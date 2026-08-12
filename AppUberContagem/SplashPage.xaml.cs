namespace AppUberContagem;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Mantém a tela de propaganda (logo) aberta por 3 segundos
        await Task.Delay(3000);

        // Verifica se o usuário já aceitou os termos anteriormente no aparelho
        bool termosAceitos = Preferences.Get("TermosAceitos", false);

        if (termosAceitos)
        {
            // Se já aceitou no passado, vai direto para o aplicativo principal
            IrParaOApp();
        }
        else
        {
            // Se for a primeira vez (ou se não aceitou), exibe o popup de termos
            PopupTermos.IsVisible = true;
        }
    }

    // Evento do botão de Ler Política
    private async void BtnLerPolitica_Clicked(object sender, EventArgs e)
    {
        // Abre o link do seu site no navegador do celular do usuário
        string url = "https://alessandrobordini.github.io/PoliticaPrivacidade/";
        await Launcher.OpenAsync(url);
    }

    // Evento do botão de Aceitar
    private void BtnAceitar_Clicked(object sender, EventArgs e)
    {
        // 1. Salva a preferência "true" no aparelho do motorista
        Preferences.Set("TermosAceitos", true);

        // 2. Esconde o popup
        PopupTermos.IsVisible = false;

        // 3. Libera o acesso ao aplicativo principal
        IrParaOApp();
    }

    // Método isolado com a sua lógica original de transição de tela
    private void IrParaOApp()
    {
        // Transiciona para a estrutura principal do aplicativo (AppShell) na janela atual
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}