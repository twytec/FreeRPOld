using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Database
{
    public interface IFrpRecordContext
    {
        public const string TableNameRecord = "Records";
        public const string JsonFieldName = "DataAsJson";

        FrpDatabase FrpDatabase { get; }

        ValueTask CreateDatabaseAsync();
        ValueTask DeleteDatabaseAsync();
        ValueTask CloseDatabaseAsync();
        ValueTask SaveChangesAsync();
        ValueTask SaveChangesAndCloseAsync();
        ValueTask AddAsync(FrpRecord record);
        ValueTask UpdateAsync(FrpRecord record);
        ValueTask DeleteAsync(FrpRecord record);
        ValueTask<FrpRecord?> FirstOrDefaultAsync(Expression<Func<FrpRecord, bool>> predExpr);
        ValueTask<FrpRecord?> FirstOrDefaultAsync(FrpQueryRequest queryRequest);
        ValueTask<FrpRecord?> FirstOrDefaultAsync(Expression<Func<FrpRecord, bool>> predExpr, FrpQueryRequest queryRequest);
        ValueTask<IEnumerable<FrpRecord>> ListOrDefaultAsync(Expression<Func<FrpRecord, bool>> predExpr);
        ValueTask<IEnumerable<FrpRecord>> ListOrDefaultAsync(FrpQueryRequest queryRequest);
        ValueTask<IEnumerable<FrpRecord>> ListOrDefaultAsync(Expression<Func<FrpRecord, bool>> predExpr, FrpQueryRequest queryRequest);

        public static FrpRecord GetRecord(string changeBy, string recordId, string recordType, string owner, string json)
        {
            return new()
            {
                DataAsJson = json,
                RecordId = recordId,
                RecordType = recordType,
                Ticks = DateTime.UtcNow.Ticks,
                ChangeBy = changeBy,
                Owner = owner
            };
        }

        public static string GetRecordType(string frpDatabaseId, string frpDatasetId) => $"{frpDatabaseId}-{frpDatasetId}";
    }
}
