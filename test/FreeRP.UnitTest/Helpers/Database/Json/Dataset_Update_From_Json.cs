using FreeRP.Database;
using FreeRP.Helpers.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.UnitTest.Helpers.Database.Json
{
    [TestClass]
    public class Dataset_Update_From_Json
    {
        private class TestModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public TestModel2 T2 { get; set; } = new();
        }

        private class TestModel2
        {
            public int Num { get; set; }
            public int[] NumArr { get; set; } = [1, 2, 3];
        }

        private class TestModelChange
        {
            public int Id { get; set; }
            public int Name { get; set; }
            public TestModel2Change T2 { get; set; } = new();
        }

        private class TestModel2Change
        {
            public int[] Num { get; set; } = [1, 2, 3];
            public string NumArr { get; set; } = string.Empty;
        }

        [TestMethod]
        public void Test()
        {
            string datasetId = "Test";

            var json = System.Text.Json.JsonSerializer.Serialize(new TestModel());
            FrpDatasetFromJson jtd = new(json);
            var ds = jtd.GetDataset(datasetId);

            var json2 = System.Text.Json.JsonSerializer.Serialize(new TestModelChange());
            FrpDatasetUpdateFromJson dufj = new(json2, ds);

            Assert.IsTrue(ds is not null);
            Assert.IsTrue(ds.DatasetId == datasetId);
            Assert.IsTrue(ds.AllowUnknownFields == false);

            Assert.IsTrue(ds.Fields.First(x => x.FieldId == "id") is FrpDataField id && id.IsPrimaryKey == true && id.DataType == FrpDatabaseDataType.FieldString);
            Assert.IsTrue(ds.Fields.First(x => x.FieldId == "name") is FrpDataField n && n.DataType == FrpDatabaseDataType.FieldNumber);

            var t2 = ds.Fields.First(x => x.FieldId == "t2");
            Assert.IsTrue(t2.DataType == FrpDatabaseDataType.FieldObject);

            Assert.IsTrue(t2.Fields.First(
                x => x.FieldId == "num") is FrpDataField nu && nu.DataType == FrpDatabaseDataType.FieldArray &&
                nu.Fields[0].DataType == FrpDatabaseDataType.FieldNumber);

            Assert.IsTrue(t2.Fields.First(x => x.FieldId == "numArr") is FrpDataField na && na.DataType == FrpDatabaseDataType.FieldString);
        }
    }
}
