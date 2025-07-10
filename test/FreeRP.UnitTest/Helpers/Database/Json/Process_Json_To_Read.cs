using FreeRP.Database;
using FreeRP.Helpers.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FreeRP.UnitTest.Helpers.Database.Json
{
    [TestClass]
    public class Process_Json_To_Read
    {
        private class TestModel
        {
            public string Id { get; set; } = "10";
            public string Name { get; set; } = "FooBar";
            public int Number { get; set; } = 10;
        }

        private readonly string _id = "10";
        private readonly TestModel _model = new();
        private string _oldJson = "";
        private readonly FrpDatabase _db = new() { DatabaseId = "db" };
        private FrpDataset _dataset = default!;
        private FrpDatabasePermissions _permission = default!;

        [TestInitialize]
        public async Task Init()
        {
            _oldJson = FreeRP.Helpers.Json.GetJson(_model);

            _dataset = await FrpDatasetFromJson.GetDatasetAsync(_oldJson, "ds");
            _db.Datasets.Add(_dataset);

            _permission = new(_db);
            _permission.All["db://db/ds/name"].ReadSelectionChanged(FrpPermissionValue.Allow);
            _permission.All["db://db/ds/number"].ReadSelectionChanged(FrpPermissionValue.Denied);
        }

        [TestMethod]
        public async Task Test()
        {
            var json = await FrpProcessJsonToRead.GetJsonAsync(_db, _dataset, _permission, _oldJson);
            Assert.IsNotNull(json);

            var j = JsonNode.Parse(json)!.AsObject()!;
            Assert.IsTrue(j.ContainsKey("id") && j["id"]!.GetValue<string>() == _id);
            Assert.IsTrue(j.ContainsKey("name") && j["name"]!.GetValue<string>() == _model.Name);
            Assert.IsFalse(j.ContainsKey("number"));
        }
    }
}
