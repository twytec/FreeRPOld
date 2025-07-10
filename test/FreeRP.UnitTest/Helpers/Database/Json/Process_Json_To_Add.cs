using FreeRP.Database;
using FreeRP.FrpServices;
using FreeRP.Helpers.Database;
using System.Text.Json.Nodes;

namespace FreeRP.UnitTest.Helpers.Database.Json
{
    [TestClass]
    public class Process_Json_To_Add
    {
        private class TestModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = "FooBar";
            public int Number { get; set; } = 10;
        }

        private readonly string _id = "10";
        private readonly TestModel _model = new();
        private string _json = "";
        private readonly FrpDatabase _db = new() { DatabaseId = "db" };
        private FrpDataset _dataset = default!;
        private FrpDatabasePermissions _permission = default!;

        [TestInitialize]
        public async Task Init()
        {
            _json = FreeRP.Helpers.Json.GetJson(_model);

            _dataset = await FrpDatasetFromJson.GetDatasetAsync(_json, "ds");
            _db.Datasets.Add(_dataset);

            _permission = new(_db);
            _permission.All["db://db/ds/name"].AddSelectionChanged(FrpPermissionValue.Allow);
            _permission.All["db://db/ds/number"].AddSelectionChanged(FrpPermissionValue.Denied);
        }

        [TestMethod]
        public async Task Test()
        {
            var json = await FrpProcessJsonToAdd.GetJsonAsync(_db, _dataset, _permission, _id, _json);
            Assert.IsNotNull(json);

            var j = JsonNode.Parse(json)!.AsObject()!;
            Assert.IsTrue(j.ContainsKey("id") && j["id"]!.GetValue<string>() == _id);
            Assert.IsTrue(j.ContainsKey("name") && j["name"]!.GetValue<string>() == _model.Name);
            Assert.IsFalse(j.ContainsKey("number"));
        }
    }
}
