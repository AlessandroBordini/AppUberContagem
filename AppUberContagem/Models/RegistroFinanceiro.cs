using SQLite;

namespace AppUberContagem.Models
{
    public class RegistroFinanceiro
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // A data exata que a corrida ou despesa aconteceu
        public DateTime Data { get; set; }

        // Vai definir se é "Ganho" ou "Gasto" (para a gente somar depois)
        public string TipoMovimentacao { get; set; }

        // Categoria do gasto/ganho (Ex: "Combustível", "Manutenção", "Corrida de App")
        public string Categoria { get; set; }

        // Um campo livre para o motorista digitar ou o sistema preencher 
        // Ex: "Gasolina do Mobi" ou "Corrida Itu-Indaiatuba"
        public string Descricao { get; set; }

        // O valor financeiro da transação
        public double Valor { get; set; }
    }
}