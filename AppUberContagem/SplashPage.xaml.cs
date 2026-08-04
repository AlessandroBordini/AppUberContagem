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

        // Mantém a tela de propaganda aberta por 3 segundos
        await Task.Delay(3000);

        // Transiciona para a estrutura principal do aplicativo (AppShell) na janela atual
        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}