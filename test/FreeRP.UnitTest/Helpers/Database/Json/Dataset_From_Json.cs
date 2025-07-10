using FreeRP.Database;
using FreeRP.Helpers.Database;

namespace FreeRP.UnitTest.Helpers.Database.Json
{
    [TestClass]
    public class Dataset_From_Json
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
            public int[] ArrNum { get; set; } = [1, 2, 3];
            public TestModel3[] ArrTest { get; set; } = [new()];
        }

        private class TestModel3
        {
            public int Num { get; set; }
        }

        [TestMethod]
        public void Test()
        {
            string datasetId = "Test";

            var json = System.Text.Json.JsonSerializer.Serialize(new TestModel());
            FrpDatasetFromJson jtd = new(json);
            var ds = jtd.GetDataset(datasetId);

            Assert.IsTrue(ds is not null);
            Assert.IsTrue(ds.DatasetId == datasetId);
            Assert.IsTrue(ds.AllowUnknownFields == false);

            Assert.IsTrue(ds.Fields.First(x => x.FieldId == "id") is FrpDataField id && id.IsPrimaryKey == true && id.DataType == FrpDatabaseDataType.FieldString);
            Assert.IsTrue(ds.Fields.First(x => x.FieldId == "name") is FrpDataField n && n.DataType == FrpDatabaseDataType.FieldString);

            var t2 = ds.Fields.First(x => x.FieldId == "t2");
            Assert.IsTrue(t2.DataType == FrpDatabaseDataType.FieldObject);
            Assert.IsTrue(t2.Fields.First(x => x.FieldId == "num") is FrpDataField nu && nu.DataType == FrpDatabaseDataType.FieldNumber);

            Assert.IsTrue(t2.Fields.First(
                x => x.FieldId == "arrNum") is FrpDataField na && na.DataType == FrpDatabaseDataType.FieldArray &&
                na.Fields[0].DataType == FrpDatabaseDataType.FieldNumber);

            Assert.IsTrue(t2.Fields.First(
                x => x.FieldId == "arrTest") is FrpDataField at && at.DataType == FrpDatabaseDataType.FieldArray &&
                at.Fields[0].DataType == FrpDatabaseDataType.FieldObject);
        }
    }
}
