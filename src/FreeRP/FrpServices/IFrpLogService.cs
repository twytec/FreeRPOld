using FreeRP.Database;
using FreeRP.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.FrpServices
{
    public interface IFrpLogService : IAsyncDisposable
    {
        public const string LogFolderName = "log";

        public const string ActionAdd = "Add";
        public const string ActionChange = "Change";
        public const string ActionDelete = "Delete";
        public const string ActionChangePasswort = "Change passwort";

        /// <summary>
        /// Adds the log to the database
        /// </summary>
        ValueTask<FrpResponse> AddLogAsync(FrpLog log, IFrpAuthService authService);

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpLog>> GetLogsAsync(FrpLogFilter filter, IFrpAuthService authService);

        /// <summary>
        /// Returns FrpLogValues
        /// </summary>
        /// <returns></returns>
        ValueTask<FrpResponse> GetLogValuesAsync(IFrpAuthService authService);

        /// <summary>
        /// Delete LogRecord from the database
        /// </summary>
        /// <param name="log"></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteLogAsync(FrpLog log, IFrpAuthService authService);

        /// <summary>
        /// Reset to the log entry
        /// </summary>
        /// <param name=""></param>
        /// <param name="authService"></param>
        /// <returns></returns>
        ValueTask<FrpResponse> ResetLogAsync(FrpLog log, IFrpAuthService authService);

        /// <summary>
        /// Adds the log to the database
        /// </summary>
        ValueTask<FrpResponse> AddEventLogAsync(FrpEventLog log, IFrpAuthService authService);

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpEventLog>> GetEventLogsAsync(FrpEventLogFilter filter, IFrpAuthService authService);

        /// <summary>
        /// Returns FrpEventLogValues
        /// </summary>
        /// <returns></returns>
        ValueTask<FrpResponse> GetEventLogValuesAsync(IFrpAuthService authService);

        /// <summary>
        /// Delete log
        /// </summary>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteEventLogAsync(FrpEventLog log, IFrpAuthService authService);

        /// <summary>
        /// Log exception
        /// </summary>
        /// <param name="location"></param>
        /// <param name="ex"></param>
        ValueTask<FrpResponse> AddExceptionLogAsync(FrpExceptionLog log, IFrpAuthService authService);

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        /// <returns></returns>
        ValueTask<IEnumerable<FrpExceptionLog>> GetExceptionLogsAsync(FrpExceptionLogFilter filter, IFrpAuthService authService);

        /// <summary>
        /// Returns GetExceptionLogValues
        /// </summary>
        /// <returns></returns>
        ValueTask<FrpResponse> GetExceptionLogValuesAsync(IFrpAuthService authService);

        /// <summary>
        /// Delete exception log
        /// </summary>
        /// <returns></returns>
        ValueTask<FrpResponse> DeleteExceptionLogAsync(FrpExceptionLog log, IFrpAuthService authService);
    }
}
