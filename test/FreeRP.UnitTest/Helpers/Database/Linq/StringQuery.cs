using FreeRP.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.UnitTest.Helpers.Database.Linq
{
    [TestClass]
    public class StringQuery
    {
        private readonly FrpDatabaseAccess _db = new(Path.GetRandomFileName(), default!, default!);

        public class TestModel
        {
            public string Name { get; set; } = "FooBar";
        }

        [TestMethod]
        public void Equals()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name == "a");
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "a" && q2.ValueType == FrpQueryType.ValueString);
        }

        [TestMethod]
        public void Equals2()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.Equals("a"));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallEquals && q1.Value == "a" && q1.ValueType == FrpQueryType.ValueString && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void EqualNot()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name != "a");
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.name" && q1.Next == FrpQueryType.QueryNotEqual);
            Assert.IsTrue(q2.Value == "a" && q2.ValueType == FrpQueryType.ValueString);
        }

        [TestMethod]
        public void EqualNot2()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.Equals("a") == false);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallEquals && q1.Value == "a" && q1.ValueType == FrpQueryType.ValueString && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void AndAlso()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name == "a" && x.Name.Length == 1);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 4);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);
            var q4 = qc1.ElementAt(3);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.name" && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "a" && q2.ValueType == FrpQueryType.ValueString && q2.Next == FrpQueryType.QueryAndAlso);

            Assert.IsTrue(
                q3.IsMember && q3.Name == "$.name" && q3.MemberType == FrpQueryType.ValueString &&
                q3.CallType == FrpQueryType.CallCount && q3.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q4.Value == "1" && q4.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void OrElse()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name == "a" || x.Name.Length == 1);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 4);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);
            var q3 = qc1.ElementAt(2);
            var q4 = qc1.ElementAt(3);

            Assert.IsTrue(q1.IsMember && q1.Name == "$.name" && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "a" && q2.ValueType == FrpQueryType.ValueString && q2.Next == FrpQueryType.QueryOrElse);

            Assert.IsTrue(
                q3.IsMember && q3.Name == "$.name" && q3.MemberType == FrpQueryType.ValueString &&
                q3.CallType == FrpQueryType.CallCount && q3.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q4.Value == "1" && q4.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void Contains()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.Contains("ab"));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallContains && q1.Value == "ab" && q1.ValueType == FrpQueryType.ValueString &&
                q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void ContainsIsFalse()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.Contains("ab") == false);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallContains && q1.Value == "ab" && q1.ValueType == FrpQueryType.ValueString);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void ContainsIsTrue()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.Contains("ab") == true);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallContains && q1.Value == "ab" && q1.ValueType == FrpQueryType.ValueString);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void Count()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.Count() == 5);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallCount && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "5" && q2.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void EndsWith()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.EndsWith("ab"));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallEndsWith && q1.Value == "ab" && q1.ValueType == FrpQueryType.ValueString &&
                q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void Length()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.Length == 5);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallCount && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "5" && q2.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void IndexOf()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.IndexOf("a") == 1);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString && q1.Value == "a" &&
                q1.CallType == FrpQueryType.CallIndexOf && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "1" && q2.ValueType == FrpQueryType.ValueNumber);
        }

        [TestMethod]
        public void IsNullOrEmpty()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => string.IsNullOrEmpty(x.Name));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallIsNullOrEmpty && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void IsNullOrEmptyIsTrue()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => string.IsNullOrEmpty(x.Name) == true);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallIsNullOrEmpty && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void IsNullOrEmptyIsFalse()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => string.IsNullOrEmpty(x.Name) == false);
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallIsNullOrEmpty && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "False" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void StartWith()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.StartsWith("ab"));
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallStartWith && q1.Value == "ab" && q1.ValueType == FrpQueryType.ValueString &&
                q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "True" && q2.ValueType == FrpQueryType.ValueBoolean);
        }

        [TestMethod]
        public void ToLower()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.ToLower() == "foobar");
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallToLower && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "foobar" && q2.ValueType == FrpQueryType.ValueString);
        }

        [TestMethod]
        public void ToUpper()
        {
            var c1 = new FrpQueryable<TestModel>(_db).Where(x => x.Name.ToUpper() == "FOOBAR");
            var qc1 = c1.GetQueries;
            Assert.IsTrue(qc1.Count() == 2);

            var q1 = qc1.ElementAt(0);
            var q2 = qc1.ElementAt(1);

            Assert.IsTrue(
                q1.IsMember && q1.Name == "$.name" && q1.MemberType == FrpQueryType.ValueString &&
                q1.CallType == FrpQueryType.CallToUpper && q1.Next == FrpQueryType.QueryEqual);
            Assert.IsTrue(q2.Value == "FOOBAR" && q2.ValueType == FrpQueryType.ValueString);
        }
    }
}
