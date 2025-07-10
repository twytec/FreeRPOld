using FreeRP.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.UnitTest.Helpers.Database.Linq
{
    [TestClass]
    public class NumberQuery
    {
        private readonly FrpDatabaseAccess _db = new(Path.GetRandomFileName(), default!, default!);

        public class TestModel
        {
            public int Id { get; set; }
        }

        [TestMethod]
        public void AddEqual()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Id + 10 == 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 3);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.id" && q1.Next == FrpQueryType.QueryAdd);
            Assert.IsTrue(q2.Value == "10" && q2.ValueType == FrpQueryType.ValueNumber && q2.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q3.Value == "0" && q3.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void SubtractEqual()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Id - 10 == 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 3);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.id" && q1.Next == FrpQueryType.QuerySubtract);
            Assert.IsTrue(q2.Value == "10" && q2.ValueType == FrpQueryType.ValueNumber && q2.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q3.Value == "0" && q3.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void MultiplyEqual()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Id * 10 == 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 3);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.id" && q1.Next == FrpQueryType.QueryMultiply);
            Assert.IsTrue(q2.Value == "10" && q2.ValueType == FrpQueryType.ValueNumber && q2.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q3.Value == "0" && q3.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void DivideEqual()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Id / 10 == 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 3);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.id" && q1.Next == FrpQueryType.QueryDivide);
            Assert.IsTrue(q2.Value == "10" && q2.ValueType == FrpQueryType.ValueNumber && q2.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q3.Value == "0" && q3.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void GreaterThan()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Id > 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.id" && q1.Next == FrpQueryType.QueryGreaterThan);
            Assert.IsTrue(q2.Value == "0" && q2.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void GreaterThanOrEqual()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Id >= 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.id" && q1.Next == FrpQueryType.QueryGreaterThanOrEqual);
            Assert.IsTrue(q2.Value == "0" && q2.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void LessThan()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Id < 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.id" && q1.Next == FrpQueryType.QueryLessThan);
            Assert.IsTrue(q2.Value == "0" && q2.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void LessThanOrEqual()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Id <= 0);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.id" && q1.Next == FrpQueryType.QueryLessThanOrEqual);
            Assert.IsTrue(q2.Value == "0" && q2.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void Equals()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Id.Equals(1) == false);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.id" && q1.MemberType == FrpQueryType.ValueNumber &&
                q1.CallType == FrpQueryType.CallEquals && q1.Value == "1" && q1.ValueType == FrpQueryType.ValueNumber && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == FrpQueryType.ValueBoolean);
        }
    }
}
