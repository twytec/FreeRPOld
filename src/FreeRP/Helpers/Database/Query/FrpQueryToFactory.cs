using FreeRP.Database;

namespace FreeRP.Helpers.Database
{
    public static class FrpQueryToFactory
    {
        public const string TableName = "Records";
        public const string JsonColName = "DataAsJson";
        public const string OwnerName = "Owner";

        public static async ValueTask<FormattableString?> GetSql(FrpDatabase db, IEnumerable<FrpQuery> queries)
        {
            switch (db.DatabaseProvider)
            {
                case DatabaseProvider.Sqlite:
                    return await FrpQueryToSqlite.GetSql(queries);
                default:
                    break;
            }
            
            return null;
        }

        public static IEnumerable<FrpQuery> Merge(IEnumerable<FrpQuery> a, IEnumerable<FrpQuery> b)
        {
            var list = a.ToList();
            list.Last().Next = FrpQueryType.QueryAndAlso;
            list.AddRange(b);
            return list;
        }
    }
}
