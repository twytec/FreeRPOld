using FreeRP.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.UnitTest.Helpers.Database.Linq
{
    [TestClass]
    public class BoolQuery
    {
        private readonly FrpDatabaseAccess _db = new(Path.GetRandomFileName(), default!, default!);

        public class TestModel
        {
            public bool Active { get; set; }
            public string Foo { get; set; } = "bar";
        }

        [TestMethod]
        public void IsFalse()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Active == false);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.active" && q1.MemberType == FrpQueryType.ValueBoolean && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void Invert()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => !x.Active);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.active" && q1.MemberType == FrpQueryType.ValueBoolean && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void IsTrue()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Active);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.active" && q1.MemberType == FrpQueryType.ValueBoolean && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == FrpQueryType.ValueBoolean);
        }
    }
}
