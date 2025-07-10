using FreeRP.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.UnitTest.Helpers.Database.Linq
{
    [TestClass]
    public class ListQuery
    {
        private readonly FrpDatabaseAccess _db = new(Path.GetRandomFileName(), default!, default!);

        public class TestModel
        {
            public List<int> IntList { get; set; } = new() { 5 };
        }

        [TestMethod]
        public void Contains()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.IntList.Contains(5));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.intList" && q1.MemberType == FrpQueryType.ValueArray &&
                q1.CallType == FrpQueryType.CallContains && q1.Value == "5" && q1.ValueType == FrpQueryType.ValueNumber &&
                q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void Count()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.IntList.Count == 5);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.intList" && q1.MemberType == FrpQueryType.ValueArray &&
                q1.CallType == FrpQueryType.CallCount && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "5" && q2.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void Index()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.IntList[1] == 5);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.intList" && q1.MemberType == FrpQueryType.ValueArray &&
                q1.Value == "1" && q1.ValueType == FrpQueryType.ValueNumber &&
                q1.CallType == FrpQueryType.CallArrayIndex && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "5" && q2.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void IndexOf()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.IntList.IndexOf(1) == 5);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.intList" && q1.MemberType == FrpQueryType.ValueArray &&
                q1.Value == "1" && q1.ValueType == FrpQueryType.ValueNumber &&
                q1.CallType == FrpQueryType.CallIndexOf && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "5" && q2.ValueType == FrpQueryType.ValueNumber);
        }
    }
}
