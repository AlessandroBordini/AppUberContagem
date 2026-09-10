namespace AppUberContagem.Models
{
    public class Viagem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Nome { get; set; } = string.Empty; // Ex: "Viagem de Trabalho (Itu / Salto / Campinas)"
        public double ValorTotalEstimado { get; set; }
        public double SubtotalPendente { get; set; }
        public bool IsFinalizada { get; set; }
        public List<ItemViagem> Itens { get; set; } = new();
    }

    public class ItemViagem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Descricao { get; set; } = string.Empty; // Ex: "Trecho: Salto até Campinas (30 km)" ou "Pedágio SP-075"
        public double Valor { get; set; }
        public int Ordem { get; set; }
        public bool IsConfirmado { get; set; }
        public string Tipo { get; set; } = "Combustivel"; // "Combustivel", "Pedagio", "Outros"

        // Propriedade para exibir direto na tela substituindo o True/False
        public string StatusTexto => IsConfirmado ? "✔️ Feito" : "⏳ Pendente";
    }
}