using AIReportsTool.API.Models;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text.Json;

namespace AIReportsTool.API.Services
{
    /// <summary>
    /// Service for handling application-related operations
    /// </summary>
    public class ApplicationService
    {
        private readonly OracleDbService _dbService;
        private readonly ILogger<ApplicationService> _logger;

        public ApplicationService(OracleDbService dbService, ILogger<ApplicationService> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        /// <summary>
        /// Gets application database details by application ID
        /// Returns empty AppDbDetailsModel with Id = 0 if validation fails
        /// </summary>
        /// <param name="appId">The application ID</param>
        /// <returns>AppDbDetailsModel containing database connection details</returns>
        public async Task<AppDbDetailsModel> GetApplicationDatabaseDetails(int appId)
        {
            try
            {
                _logger.LogInformation("Getting database details for AppId: {AppId}", appId);

                // Create parameters for stored procedure
                var appIdParam = OracleDbService.CreateInputParameter(
                    "p_app_id",
                    OracleDbType.Int32,
                    appId
                );

                var refCursorParam = OracleDbService.CreateRefCursorParameter("p_result");

                // Execute stored procedure
                var dataTable = await _dbService.ExecuteStoredProcedureAsync(
                    "P_IRT_REPORTHELPERTOOL.SP_GetApplicationDbDetailsByAppId",
                    appIdParam,
                    refCursorParam
                );

                // Validation 1: Check if dataTable is empty or has 0 rows
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    _logger.LogWarning("No database details found for AppId: {AppId}", appId);
                    return new AppDbDetailsModel { Id = 0 };
                }

                // Get the first row
                var dataRow = dataTable.Rows[0];

                // Validation 2: Check if ID is 0 or APPID is 0
                var id = OracleDbService.GetColumnValue<int>(dataRow, "ID", 0);
                var dbAppId = OracleDbService.GetColumnValue<int>(dataRow, "APPID", 0);

                if (id == 0 || dbAppId == 0)
                {
                    _logger.LogWarning("Invalid ID or APPID in database details for AppId: {AppId}", appId);
                    return new AppDbDetailsModel { Id = 0 };
                }

                // Validation 3: Check DBPROVIDER
                var dbProvider = OracleDbService.GetColumnValue<string>(dataRow, "DBPROVIDER", string.Empty) ?? string.Empty;
                dbProvider = dbProvider.Trim().ToUpperInvariant();

                if (string.IsNullOrEmpty(dbProvider) ||
                    (dbProvider != "ORACLE" && dbProvider != "SQLSERVER" && dbProvider != "MYSQL"))
                {
                    _logger.LogWarning("Invalid DBPROVIDER '{DbProvider}' for AppId: {AppId}", dbProvider, appId);
                    return new AppDbDetailsModel { Id = 0 };
                }

                // Validation 4: Check DBCONNECTION (CLOB)
                var dbConnection = OracleDbService.GetColumnValue<string>(dataRow, "DBCONNECTION", string.Empty) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(dbConnection))
                {
                    _logger.LogWarning("Empty DBCONNECTION for AppId: {AppId}", appId);
                    return new AppDbDetailsModel { Id = 0 };
                }

                // Validation 5: Check DBSTRUCTUREJSON (CLOB and valid JSON)
                var dbStructureJson = OracleDbService.GetColumnValue<string>(dataRow, "DBSTRUCTUREJSON", string.Empty) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(dbStructureJson))
                {
                    _logger.LogWarning("Empty DBSTRUCTUREJSON for AppId: {AppId}", appId);
                    return new AppDbDetailsModel { Id = 0 };
                }

                // Validate JSON format
                try
                {
                    using var jsonDoc = JsonDocument.Parse(dbStructureJson);
                    // JSON is valid
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid JSON in DBSTRUCTUREJSON for AppId: {AppId}", appId);
                    return new AppDbDetailsModel { Id = 0 };
                }

                // All validations passed - create and return the model
                var appDb = new AppDbDetailsModel
                {
                    Id = id,
                    AppId = dbAppId,
                    DbProvider = dbProvider,
                    DbConnection = dbConnection,
                    DbStructureJson = dbStructureJson
                };

                _logger.LogInformation("Successfully retrieved database details for AppId: {AppId}", appId);
                return appDb;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database details for AppId: {AppId}", appId);
                return new AppDbDetailsModel { Id = 0 };
            }
        }
    }
}
