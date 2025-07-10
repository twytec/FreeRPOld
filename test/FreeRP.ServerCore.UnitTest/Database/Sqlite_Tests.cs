using FreeRP.Database;
using FreeRP.Helpers;
using FreeRP.ServerCore.Database;
using FreeRP.Settings;

namespace FreeRP.ServerCore.UnitTest.Database
{
    [TestClass]
    public class Sqlite_Tests
    {
        private readonly FrpSettings _frpSettings = MySettings.FrpSettings;
        private readonly FrpDatabaseAccess _db;
        private readonly IFrpRecordContext _rc;
        private class MyTest
        {
            public string Id { get; set; } = string.Empty;
            public string FooBar { get; set; } = string.Empty;
            public int Number { get; set; }
            public bool Foo { get; set; }
            public int[]? MoneyArr { get; set; }
            public List<int>? ListArr { get; set; }
            public MyTest2? Obj { get; set; }
        }

        private class MyTest2
        {
            public string? Text { get; set; }
            public int Number { get; set; }
        }

        public Sqlite_Tests()
        {
            _db = new FrpDatabaseAccess(Path.GetRandomFileName(), MySettings.FrpDataService, MySettings.TestAdmin);
            _rc = FrpRecordContextFactory.Create(_frpSettings, new FrpDatabase() { DatabaseId = _db.FrpDatabaseId });
        }

        [TestInitialize]
        public async Task InitDatabaseAsync()
        {
            await _rc.CreateDatabaseAsync();
            for (int i = 0; i < 10; i++)
            {
                string userId = $"user{i}";
                MyTest test = new()
                {
                    Foo = true,
                    Id = i.ToString(),
                    FooBar = $"FooBar{i}",
                    MoneyArr = [i],
                    ListArr = [i],
                    Number = i,
                    Obj = new() { Text = i.ToString(), Number = i }
                };

                await _rc.AddAsync(IFrpRecordContext.GetRecord(userId, i.ToString(), nameof(MyTest), "Test", Json.GetJson(test)));
            }
            await _rc.SaveChangesAsync();
        }

        [TestMethod]
        public async Task ArrayQueryAsync()
        {
            var q = new FrpQueryable<MyTest>(_db).Where(x => x.MoneyArr!.Contains(5));
            FrpQueryRequest qr = new() { DatabaseId = _db.FrpDatabaseId, DatasetId = "MyTest" };

            qr.Queries.AddRange(q.GetQueries);
            var data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "5");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.MoneyArr!.Contains(5) == false);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 9);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.MoneyArr![0] == 5);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.MoneyArr!.Length == 1);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 10);
        }

        [TestMethod]
        public async Task ListQueryAsync()
        {
            FrpQueryRequest qr = new() { DatabaseId = _db.FrpDatabaseId, DatasetId = "MyTest" };

            var q = new FrpQueryable<MyTest>(_db).Where(x => x.ListArr!.Contains(5));
            qr.Queries.AddRange(q.GetQueries);
            var data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "5");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.ListArr![0] == 5);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.ListArr!.IndexOf(0) == 5);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.ListArr!.Contains(5) == false);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 9);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.ListArr!.Count == 1);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 10);
        }

        [TestMethod]
        public async Task NuberQueryAsync()
        {
            FrpQueryRequest qr = new() { DatabaseId = _db.FrpDatabaseId, DatasetId = "MyTest" };

            var q = new FrpQueryable<MyTest>(_db).Where(x => x.Number + 1 == 2);
            qr.Queries.AddRange(q.GetQueries);
            var data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "1");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.Number - 1 == 2);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "3");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.Number * 2 == 4);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "2");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.Number / 2 == 3);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 2 && data[0].RecordId == "6");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.Number > 1);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 8);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.Number >= 1);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 9);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.Number < 1);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.Number <= 1);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 2);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.Number == 1);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "1");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.Number != 1);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 9);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.Number.Equals(1));
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "1");
            qr.Queries.Clear();

            //q = new FrpQueryable<MyTest>(_db).Where(x => x.Number.Equals(1) == false);
            //fs = _queryToSql.CreateSql(q.GetQueries, owner: "Test");
            //data = await _rc.Records.FromSql(fs).ToListAsync();
            //Assert.IsTrue(data.Count is 9);
        }

        [TestMethod]
        public async Task StringQuery()
        {
            FrpQueryRequest qr = new() { DatabaseId = _db.FrpDatabaseId, DatasetId = "MyTest" };

            var q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar == "FooBar1");
            qr.Queries.AddRange(q.GetQueries);
            var data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "1");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar != "FooBar1");
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 9);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.Equals("FooBar1"));
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "1");
            qr.Queries.Clear();

            //q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.Equals("FooBar1") == false);
            //fs = _queryToSql.CreateSql(q.GetQueries, owner: "Test");
            //data = await _rc.Records.FromSql(fs).ToListAsync();
            //Assert.IsTrue(data.Count is 9);

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.IndexOf("1") == 6);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "1");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => string.IsNullOrEmpty(x.FooBar));
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 0);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => string.IsNullOrEmpty(x.FooBar) == false);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 10);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.Count() == 7);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 10);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.Length == 7);
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 10);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.StartsWith("Foo"));
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 10);
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.EndsWith("1"));
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "1");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.ToLower() == "foobar1");
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "1");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.ToUpper() == "FOOBAR1");
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "1");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.StartsWith("F") && x.FooBar.EndsWith("1"));
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 1 && data[0].RecordId == "1");
            qr.Queries.Clear();

            q = new FrpQueryable<MyTest>(_db).Where(x => x.FooBar.StartsWith("F") || x.FooBar.EndsWith("1"));
            qr.Queries.AddRange(q.GetQueries);
            data = (await _rc.ListOrDefaultAsync(qr)).ToList();
            Assert.IsTrue(data.Count is 10);
        }
    }
}
