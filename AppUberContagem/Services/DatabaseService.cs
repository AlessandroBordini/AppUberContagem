using SQLite;
using AppUberContagem.Models;

namespace AppUberContagem.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _db;

        async Task<SQLiteAsyncConnection> GetDbAsync()
        {
            if (_db is not null)
                return _db;

            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "FinancasMotorista.db3");
            _db = new SQLiteAsyncConnection(databasePath);
            await _db.CreateTableAsync<RegistroFinanceiro>();
            return _db;
        }

        public async Task<int> SalvarRegistroAsync(RegistroFinanceiro registro)
        {
            var db = await GetDbAsync();
            return await db.InsertAsync(registro);
        }

        public async Task<List<RegistroFinanceiro>> ObterRegistrosAsync()
        {
            var db = await GetDbAsync();
            return await db.Table<RegistroFinanceiro>().ToListAsync();
        }

        public async Task<List<RegistroFinanceiro>> ObterRegistrosPorDataAsync(DateTime dataInicio, DateTime dataFim)
        {
            var db = await GetDbAsync();
            return await db.Table<RegistroFinanceiro>()
                            .Where(r => r.Data >= dataInicio && r.Data <= dataFim)
                            .ToListAsync();
        }

        public async Task<int> DeletarRegistroAsync(int id)
        {
            var db = await GetDbAsync();
            return await db.DeleteAsync<RegistroFinanceiro>(id);
        }
    }
}
