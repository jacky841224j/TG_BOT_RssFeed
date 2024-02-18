using Dapper;
using Microsoft.Data.Sqlite;

namespace TGBot_RssFeed_Polling.Services
{
    public class InitService : IHostedService
    {
        private readonly ILogger<InitService> _logger;
        private readonly SqliteConnection _sqlcon;
        private readonly string SavePath = Environment.CurrentDirectory + "/Repositories/RssFeed.db";

        public InitService(ILogger<InitService> logger, SqliteConnection sqlcon)
        {
            _logger = logger;
            _sqlcon = sqlcon;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Directory.Exists(Environment.CurrentDirectory + "/Repositories"))
            {
                _logger.LogInformation("建立資料夾...");
                Directory.CreateDirectory(Environment.CurrentDirectory + "/Repositories");
            }

            if (!File.Exists(SavePath))
            {
                using (_sqlcon)
                {
                    await _sqlcon.OpenAsync();
                    _ = await _sqlcon.ExecuteAsync(
                        @"CREATE TABLE User (
                       ID INTEGER ,
                       UserID  TEXT NOT NULL UNIQUE,
                       PRIMARY KEY(ID AUTOINCREMENT)
                    );"
                    );

                    _ = await _sqlcon.ExecuteAsync(
                        @"CREATE TABLE Sub (
                       ID INTEGER ,
                       Num INTEGER ,
                       UserID  TEXT NOT NULL ,
                       SubTitle  TEXT ,
                       SubUrl  TEXT NOT NULL UNIQUE,
                       PRIMARY KEY(ID AUTOINCREMENT)
                    );"
                    );

                    _ = await _sqlcon.ExecuteAsync(
                        @"CREATE TABLE Time (
                        ID INTEGER ,
                        UpdateTime TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        PRIMARY KEY(ID AUTOINCREMENT)
                    );"
                    );

                    _ = await _sqlcon.ExecuteAsync(@"INSERT INTO Time (UpdateTime) values (@Time)", new { Time = DateTime.Now });
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }
}
