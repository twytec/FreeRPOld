// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using FreeRP.Benchmark.Dev;

#if DEBUG
//var sq = new SqliteTest();
//await sq.Setup();
//await sq.Add1000ItemsSqliteJson();
//await sq.WhereSqliteJson();

#else
//var sq = new SqliteTest();
//await sq.Add1000ItemsFrpDbSqlite();
//await sq.FirstOrDefaultFrpDbSqlite();

Console.WriteLine($"1: {nameof(SqliteTest)}");

switch (Console.ReadLine())
{
    case "1":
        BenchmarkRunner.Run<SqliteTest>();
        break;
    case "2":
        break;
    default:
        break;
}

#endif