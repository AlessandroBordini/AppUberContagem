using Microsoft.Maui.Storage;
using System.Threading.Tasks;

namespace AppUberContagem.Helpers;

public static class PremiumHelper
{
    private const string ChavePremium = "usuario_e_premium";

    // Método centralizado para saber se é Premium
    public static async Task<bool> IsPremiumAsync()
    {
        // 1. Tenta pegar do cofre seguro
        string salvoNoCofre = await SecureStorage.Default.GetAsync(ChavePremium);

        // 2. Se estiver no cofre, é Premium
        if (salvoNoCofre == "sim")
            return true;

        // 3. Fallback (retrocompatibilidade): 
        // Se ainda não migrou no SecureStorage mas está no Preferences antigo
        if (Preferences.Default.Get("IsPremium", false))
        {
            // Migra automaticamente para o SecureStorage
            await SecureStorage.Default.SetAsync(ChavePremium, "sim");
            return true;
        }

        return false;
    }
}