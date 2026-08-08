using SQLite;
using AppUberContagem.Models;

namespace AppUberContagem.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _db;

        // Inicializa o banco de dados e cria a tabela caso ela não exista
        async Task Init()
        {
            if (_db != null)
                return;

            // Define o caminho onde o arquivo do banco ficará salvo fisicamente no celular
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "FinancasMotorista.db3");
            _db = new SQLiteAsyncConnection(databasePath);

            // Cria a tabela baseada no modelo
            await _db.CreateTableAsync<RegistroFinanceiro>();
        }

        // Método que usaremos para salvar qualquer ganho ou gasto
        public async Task<int> SalvarRegistroAsync(RegistroFinanceiro registro)
        {
            await Init();
            return await _db.InsertAsync(registro);
        }

        // Método para buscar todos os registros cadastrados (usado no relatório)
        public async Task<List<RegistroFinanceiro>> ObterRegistrosAsync()
        {
            await Init();
            return await _db.Table<RegistroFinanceiro>().ToListAsync();
        }

        // Método para os Relatórios por Período
        public async Task<List<RegistroFinanceiro>> ObterRegistrosPorDataAsync(DateTime dataInicio, DateTime dataFim)
        {
            await Init();
            return await _db.Table<RegistroFinanceiro>()
                            .Where(r => r.Data >= dataInicio && r.Data <= dataFim)
                            .ToListAsync();
        }

        // Método para deletar um registro pelo ID (Adicionado)
        public async Task<int> DeletarRegistroAsync(int id)
        {
            await Init();
            return await _db.DeleteAsync<RegistroFinanceiro>(id);
        }
    }
}