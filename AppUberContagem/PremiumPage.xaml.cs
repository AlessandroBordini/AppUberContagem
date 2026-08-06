using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using AppUberContagem.Services;

namespace AppUberContagem
{
    public partial class PremiumPage : ContentPage
    {
        private readonly BillingService _billingService = new BillingService();

        // Uma "chave" para salvar no celular que o usuário comprou, para o app funcionar offline
        private const string ChavePremium = "usuario_e_premium";

        public PremiumPage()
        {
            InitializeComponent();
        }

        // Toda vez que a tela aparece, ele faz essa checagem
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ChecarSeJaEPremium();
        }

        private async Task ChecarSeJaEPremium()
        {
            IndicadorCarregamento.IsVisible = true;
            IndicadorCarregamento.IsRunning = true;

            // 1. Olha na memória interna do celular se a compra já está salva
            string salvoNoCelular = await SecureStorage.Default.GetAsync(ChavePremium);

            if (salvoNoCelular == "sim")
            {
                // Sincroniza com a chave que a MainPage e LancamentosPage lêem
                Preferences.Default.Set("IsPremium", true);
                MudarTelaParaPremium();
            }
            else
            {
                // 2. Se não tem no celular, consulta o Google Play (requer internet)
                bool comprouNoGoogle = await _billingService.IsPremiumPurchasedAsync();

                if (comprouNoGoogle)
                {
                    await SecureStorage.Default.SetAsync(ChavePremium, "sim");
                    Preferences.Default.Set("IsPremium", true); // Sincroniza
                    MudarTelaParaPremium();
                }
            }

            IndicadorCarregamento.IsVisible = false;
            IndicadorCarregamento.IsRunning = false;
        }

        // Evento de clique do botão Comprar
        private async void OnComprarClicked(object sender, EventArgs e)
        {
            BtnComprar.IsEnabled = false; // Desativa o botão temporariamente para não clicar duas vezes
            IndicadorCarregamento.IsVisible = true;
            IndicadorCarregamento.IsRunning = true;

            // Chama o serviço de compra que criamos
            bool compraSucesso = await _billingService.BuyPremiumAsync();

            if (compraSucesso)
            {
                // Se a compra deu certo, salva no celular que ele é Premium!
                await SecureStorage.Default.SetAsync(ChavePremium, "sim");
                Preferences.Default.Set("IsPremium", true); // Sincroniza com as outras telas
                MudarTelaParaPremium();
            }

            IndicadorCarregamento.IsVisible = false;
            IndicadorCarregamento.IsRunning = false;
            BtnComprar.IsEnabled = true;
        }

        // Função para esconder o botão de compra e mostrar a mensagem de sucesso
        private void MudarTelaParaPremium()
        {
            BtnComprar.IsVisible = false;
            LblSucesso.IsVisible = true;
        }
    }
}