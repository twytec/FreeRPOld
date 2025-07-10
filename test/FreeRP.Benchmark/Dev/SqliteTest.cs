using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using FreeRP.Database;
using FreeRP.Helpers;
using FreeRP.ServerCore;
using Microsoft.EntityFrameworkCore;

namespace FreeRP.Benchmark.Dev
{
    [SimpleJob(RunStrategy.Monitoring, RuntimeMoniker.Net80)]
    public class SqliteTest
    {
        private const int AppItemCount = 10000;
        private const int TestItemCount = 50;

        private FrpSettings _frpSettings = default!;
        private FrpDataService _frpDataService = default!;
        private FrpAuthService _testAdmin = default!;
        private FrpAuthService _testUser = default!;
        private FrpDatabaseAccess _db = default!;

        private SqliteContext _sqliteContext = default!;
        private SqliteJsonContext _jsonContext = default!;

        private class TestModel
        {
            public string Id { get; set; } = string.Empty;
            public int Number { get; set; }
            public string Json { get; set; } = string.Empty;
        }

        public SqliteTest()
        {

        }

        #region Sqlite setup

        private class SqliteContext : DbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder options)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{Guid.NewGuid()}.db");
                options.UseSqlite($"Data Source={path}");
            }

            public DbSet<TestModel> Tests { get; set; }
        }

        private void SqliteSetup()
        {
            _sqliteContext = new SqliteContext();
            _sqliteContext.Database.EnsureCreated();
        }

        #endregion

        #region Sqlite json setup

        private class SqliteJsonContext : DbContext
        {
            protected override void OnConfiguring(DbContextOptionsBuilder options)
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{Guid.NewGuid()}.db");
                options.UseSqlite($"Data Source={path}");
            }

            public DbSet<TestModel> Tests { get; set; }
        }

        private void SqliteJsonSetup()
        {
            _jsonContext = new SqliteJsonContext();
            _jsonContext.Database.EnsureCreated();
        }

        #endregion

        #region FreeRP setup

        private async Task FrpSetup()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FrpSettings.DataFolderName);
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            _frpSettings = new();
            _frpSettings.SetPaths(AppDomain.CurrentDomain.BaseDirectory);
            _frpSettings.LogDatabase = false;

            _frpDataService = new(new MyWebHostEnvironment(), _frpSettings, new Microsoft.Extensions.Logging.Abstractions.NullLogger<FrpDataService>());

            _testAdmin = new(_frpDataService, new()) { IsAdmin = true, User = new() { UserId = _frpSettings.AdminId } };
            _testUser = new(_frpDataService, new()) { User = new() { UserId = "TestUser" } };

            FrpDatabase db = new()
            {
                DatabaseId = Path.GetRandomFileName(),
                AccessMode = AccessMode.Custom,
                AllowUnknownData = true
            };

            var json = Json.GetJson(new TestModel());
            var dataset = await Helpers.Database.FrpDatasetFromJson.GetDatasetAsync(json, nameof(TestModel));
            db.Datasets.Add(dataset);

            await _frpDataService.FrpDatabaseService.AddDatabaseAsync(db, _testAdmin);
            _db = new(db.DatabaseId, _frpDataService, _testUser);
            await _db.OpenDatabaseAsync();
        }

        #endregion

        [GlobalSetup]
        public async Task Setup()
        {
            SqliteSetup();
            SqliteJsonSetup();
            await FrpSetup();

            await Add10000ItemsSqlite();
            await Add10000ItemsSqliteJson();
            await Add10000ItemsFrpDbSqlite();
        }

        [Benchmark(0)]
        public async Task Add10000ItemsSqlite()
        {
            for (int i = 0; i < AppItemCount; i++)
            {
                TestModel model = new() { Id = Guid.NewGuid().ToString(), Number = i, Json = i.ToString() };
                await _sqliteContext.Tests.AddAsync(model);
            }
            await _sqliteContext.SaveChangesAsync();
        }

        [Benchmark(1)]
        public async Task Add10000ItemsSqliteJson()
        {
            for (int i = 0; i < AppItemCount; i++)
            {
                TestModel model = new() { Id = Guid.NewGuid().ToString(), Number = i };
                model.Json = System.Text.Json.JsonSerializer.Serialize(model);

                await _jsonContext.Tests.AddAsync(model);
            }
            await _jsonContext.SaveChangesAsync();
        }

        [Benchmark(2)]
        public async Task Add10000ItemsFrpDbSqlite()
        {
            for (int i = 0; i < AppItemCount; i++)
            {
                TestModel model = new() { Id = Guid.NewGuid().ToString(), Number = i };
                await _db.AddAsync(model);
            }
            await _db.SaveChangesAsync();
        }

        [Benchmark(3)]
        public async Task FirstOrDefaultSqlite()
        {
            await Task.Delay(100);
            for (int i = 0; i < TestItemCount; i++)
            {
                var m = await _sqliteContext.Tests.FirstOrDefaultAsync(x => x.Number == 1000 + i);
            }

        }

        [Benchmark(4)]
        public async Task FirstOrDefaultSqliteJson()
        {
            await Task.Delay(100);
            for (int i = 0; i < TestItemCount; i++)
            {
                var m = await _jsonContext.Tests
                    .FromSql($"select * from Tests where json_extract(Tests.Json, '$.Number') = {1000 + i}")
                    .FirstOrDefaultAsync();
            }
        }

        [Benchmark(5)]
        public async Task FirstOrDefaultFrpDbSqlite()
        {
            await Task.Delay(100);
            for (int i = 0; i < TestItemCount; i++)
            {
                int num = 1000 + i;
                var m = await _db.FirstOrDefaultAsync<TestModel>(x => x.Number == num);
            }
        }

        [Benchmark(6)]
        public async Task WhereSqlite()
        {
            await Task.Delay(100);
            for (int i = 0; i < TestItemCount; i++)
            {
                var m = await _sqliteContext.Tests.Where(x => x.Number > 1000 + i).ToListAsync();
            }
        }

        [Benchmark(7)]
        public async Task WhereSqliteJson()
        {
            await Task.Delay(100);
            for (int i = 0; i < TestItemCount; i++)
            {
                int num = 1000 + i;
                var m = await _jsonContext.Tests
                    .FromSql($"select * from Tests where json_extract(Tests.Json, '$.Number') > {num}")
                    .ToListAsync();

                foreach (var item in m)
                {
                    _ = System.Text.Json.JsonSerializer.Deserialize<TestModel>(item.Json);
                }
            }
        }

        [Benchmark(8)]
        public async Task WhereFrpDbSqlite()
        {
            await Task.Delay(100);
            for (int i = 0; i < TestItemCount; i++)
            {
                int num = 1000 + i;
                var m = await _db.Where<TestModel>(x => x.Number > num).ToListAsync();
            }
        }
    }
}
