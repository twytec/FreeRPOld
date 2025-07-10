using FreeRP.Database;
using FreeRP.ServerCore.Settings;
using FreeRP.Settings;

namespace FreeRP.ServerCore.Database
{
    public static class FrpRecordContextFactory
    {
        public static IFrpRecordContext Create(FrpSettings settings, FrpDatabase database)
        {
            return database.DatabaseProvider switch
            {
                DatabaseProvider.Sqlite => new EfRecordContext(settings, database),
                _ => throw new NotSupportedException(),
            };
        }
    }
}
