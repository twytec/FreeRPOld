using FreeRP.Database;
using FreeRP.Helpers.Database;
using FreeRP.ServerCore.Settings;
using FreeRP.Settings;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

namespace FreeRP.ServerCore.Database
{
    public class EfRecordContext(FrpSettings frpSettings, FrpDatabase config) : DbContext, IFrpRecordContext
    {
        private readonly FrpSettings _frpSettings = frpSettings;
        private readonly FrpDatabase _db = config;

        public FrpDatabase FrpDatabase { get; } = config;

        #region IFrpRecordContext

        public async ValueTask CreateDatabaseAsync() => await Database.EnsureCreatedAsync();
        public async ValueTask DeleteDatabaseAsync() => await Database.EnsureDeletedAsync();
        public async ValueTask CloseDatabaseAsync() => await DisposeAsync();
        async ValueTask IFrpRecordContext.SaveChangesAsync() => await SaveChangesAsync();
        public async ValueTask SaveChangesAndCloseAsync()
        {
            await SaveChangesAsync();
            await CloseDatabaseAsync();
        }

        public ValueTask AddAsync(FrpRecord record)
        {
            if (string.IsNullOrEmpty(record.RecordId))
                throw new MissingPrimaryKeyException();

            Records.Add(record);

            return ValueTask.CompletedTask;
        }

        public ValueTask UpdateAsync(FrpRecord record)
        {
            if (string.IsNullOrEmpty(record.RecordId))
                throw new MissingPrimaryKeyException();

            Records.Update(record);

            return ValueTask.CompletedTask;
        }

        public ValueTask DeleteAsync(FrpRecord record)
        {
            if (string.IsNullOrEmpty(record.RecordId))
                throw new MissingPrimaryKeyException();

            Records.Remove(record);

            return ValueTask.CompletedTask;
        }

        public async ValueTask<FrpRecord?> FirstOrDefaultAsync(Expression<Func<FrpRecord, bool>> predExpr)
        {
            return await Records.FirstOrDefaultAsync(predExpr);
        }

        public async ValueTask<FrpRecord?> FirstOrDefaultAsync(FrpQueryRequest queryRequest)
        {
            var q = await GetQueryAsync(queryRequest);
            return await q.FirstOrDefaultAsync();
        }

        public async ValueTask<FrpRecord?> FirstOrDefaultAsync(Expression<Func<FrpRecord, bool>> predExpr, FrpQueryRequest queryRequest)
        {
            var q = await GetQueryAsync(queryRequest);
            return await q.FirstOrDefaultAsync(predExpr);
        }

        public async ValueTask<IEnumerable<FrpRecord>> ListOrDefaultAsync(Expression<Func<FrpRecord, bool>> predExpr)
        {
            return await Records.Where(predExpr).ToArrayAsync();
        }

        public async ValueTask<IEnumerable<FrpRecord>> ListOrDefaultAsync(FrpQueryRequest queryRequest)
        {
            var q = await GetQueryAsync(queryRequest);
            return await q.ToArrayAsync();
        }

        public async ValueTask<IEnumerable<FrpRecord>> ListOrDefaultAsync(Expression<Func<FrpRecord, bool>> predExpr, FrpQueryRequest queryRequest)
        {
            var q = await GetQueryAsync(queryRequest);
            return await q.Where(predExpr).ToArrayAsync();
        }

        private async ValueTask<IQueryable<FrpRecord>> GetQueryAsync(FrpQueryRequest queryRequest)
        {
            IQueryable<FrpRecord> q = Records.AsQueryable();

            var sql = await FrpQueryToFactory.GetSql(FrpDatabase, queryRequest.Queries);

            if (string.IsNullOrEmpty(queryRequest.DatabaseId) == false && string.IsNullOrEmpty(queryRequest.DatasetId) == false)
                q = q.Where(x => x.RecordType == IFrpRecordContext.GetRecordType(queryRequest.DatabaseId, queryRequest.DatasetId));

            if (string.IsNullOrEmpty(FrpDatabase.Owner) == false)
                q = q.Where(x => x.Owner == FrpDatabase.Owner);

            if (sql is not null)
                q = Records.FromSql(sql);

            if (queryRequest.Take > 0)
                q = q.Take(queryRequest.Take);

            if (queryRequest.Skipe > 0)
                q = q.Skip(queryRequest.Skipe);

            return q;
        }

        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_db.DatabaseProvider == DatabaseProvider.Sqlite)
            {
                string path = Path.Combine(_frpSettings.DatabaseSettings.DatabaseRootPath, $"{_db.DatabaseId}.db");
                options.UseSqlite($"Data Source={path}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FrpRecord>().HasKey(x => x.RecordId);
            modelBuilder.Entity<FrpRecord>().HasIndex(x => new { x.RecordId, x.RecordType, x.ChangeBy, x.Owner, x.Ticks });

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<FrpRecord> Records { get; set; }
    }
}
