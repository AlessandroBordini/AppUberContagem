using Plugin.InAppBilling;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace AppUberContagem.Services
{
    public class BillingService
    {
        // Substitua pelo ID exato da opção de compra que você definiu no Play Console (ex: "premium")
        private const string PremiumProductId = "calculadora_corrida_premium";

        /// <summary>
        /// Consulta os servidores do Google Play para saber se o motorista já comprou o Premium.
        /// </summary>
        public async Task<bool> IsPremiumPurchasedAsync()
        {
            var billing = CrossInAppBilling.Current;
            try
            {
                var connected = await billing.ConnectAsync();
                if (!connected)
                    return false;

                // Busca as compras efetuadas do tipo InApp (não-consumível)
                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);

                if (purchases != null)
                {
                    return purchases.Any(p => p.ProductId == PremiumProductId && p.State == PurchaseState.Purchased);
                }
            }
            catch (InAppBillingPurchaseException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Billing] Erro ao checar compras: {ex.PurchaseError} - {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Billing] Erro genérico: {ex.Message}");
            }
            finally
            {
                await billing.DisconnectAsync();
            }

            return false;
        }

        /// <summary>
        /// Abre a tela nativa do Google Play para o motorista realizar a compra do Premium.
        /// </summary>
        public async Task<bool> BuyPremiumAsync()
        {
            var billing = CrossInAppBilling.Current;
            try
            {
                var connected = await billing.ConnectAsync();
                if (!connected)
                {
                    await Shell.Current.DisplayAlert("Conexão", "Não foi possível conectar à loja do Google Play.", "OK");
                    return false;
                }

                // Processa a compra
                var purchase = await billing.PurchaseAsync(PremiumProductId, ItemType.InAppPurchase);

                if (purchase != null && purchase.State == PurchaseState.Purchased)
                {
                    await Shell.Current.DisplayAlert("Parabéns!", "Versão Premium ativada com sucesso!", "OK");
                    return true;
                }
            }
            catch (InAppBillingPurchaseException ex)
            {
                if (ex.PurchaseError == PurchaseError.UserCancelled)
                {
                    // Motorista apenas cancelou/fechou a tela de pagamento
                }
                else if (ex.PurchaseError == PurchaseError.AlreadyOwned)
                {
                    await Shell.Current.DisplayAlert("Aviso", "Você já possui este item ativado na sua conta.", "OK");
                    return true;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Erro na Compra", $"Ocorreu um erro: {ex.PurchaseError}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erro", $"Falha ao processar compra: {ex.Message}", "OK");
            }
            finally
            {
                await billing.DisconnectAsync();
            }

            return false;
        }
    }
}