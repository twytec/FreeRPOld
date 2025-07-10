using FreeRP.Database;
using FreeRP.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.UnitTest.Helpers.Json
{
    [TestClass]
    public class JsonMapper_Test
    {
        private class TestModel
        {
            public string Id { get; set; } = "1";
            public string Name { get; set; } = string.Empty;
            public int Number { get; set; } = 10;
            public int[] NumArr { get; set; } = [1, 2, 3];
            public bool Bo { get; set; } = true;
            public TestObj TestObj { get; set; } = new();
        }

        private class TestObj
        {
            public string Name { get; set; } = string.Empty;
        }

        [TestMethod]
        public void Test()
        {
            var json = FreeRP.Helpers.Json.GetJson(new TestModel());
            var map = new FrpJsonMapper(json, "db://test/test");

            Assert.IsTrue(map.GetValue("db://test/test/id") is FrpJsonMapper.JsonMapperValue id
                && id.DataType == FrpDatabaseDataType.FieldString && id.Val == "1");

            Assert.IsTrue(map.GetValue("db://test/test/name") is FrpJsonMapper.JsonMapperValue name
                && name.DataType == FrpDatabaseDataType.FieldString && name.Val == "");

            Assert.IsTrue(map.GetValue("db://test/test/number") is FrpJsonMapper.JsonMapperValue number
                && number.DataType == FrpDatabaseDataType.FieldNumber && number.Val == "10");

            Assert.IsTrue(map.GetValue("db://test/test/numArr") is FrpJsonMapper.JsonMapperValue numArr
                && numArr.DataType == FrpDatabaseDataType.FieldArray && numArr.Val == "[1,2,3]");

            Assert.IsTrue(map.GetValue("db://test/test/bo") is FrpJsonMapper.JsonMapperValue bo
                && bo.DataType == FrpDatabaseDataType.FieldBoolean && bo.Val == "true");

            Assert.IsTrue(map.GetValue("db://test/test/testObj") is FrpJsonMapper.JsonMapperValue testObj
                && testObj.DataType == FrpDatabaseDataType.FieldObject && testObj.Val == "{\"name\":\"\"}");
        }
    }
}
