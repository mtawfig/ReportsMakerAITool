using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace AIReportsTool.API.Services
{
    public class OracleDbService
    {
        private readonly string _connectionString;
        private readonly ILogger<OracleDbService> _logger;

        public OracleDbService(IConfiguration configuration, ILogger<OracleDbService> logger)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection")
                ?? throw new ArgumentNullException("Oracle connection string is not configured");
            _logger = logger;
        }

        /// <summary>
        /// Executes a stored procedure and returns a DataTable from the REF CURSOR
        /// </summary>
        public async Task<DataTable> ExecuteStoredProcedureAsync(string procedureName, params OracleParameter[] parameters)
        {
            var dataTable = new DataTable();

            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Add parameters
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                // Execute and read from REF CURSOR
                using var adapter = new OracleDataAdapter(command);
                await Task.Run(() => adapter.Fill(dataTable));

                _logger.LogInformation($"Successfully executed {procedureName}, returned {dataTable.Rows.Count} rows");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing stored procedure {procedureName}");
                throw;
            }

            return dataTable;
        }

        /// <summary>
        /// Executes a stored procedure with OUT parameters (for INSERT/UPDATE operations)
        /// </summary>
        public async Task<Dictionary<string, object?>> ExecuteStoredProcedureWithOutParamsAsync(
            string procedureName,
            params OracleParameter[] parameters)
        {
            var outParameters = new Dictionary<string, object?>();

            try
            {
                using var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Add parameters
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                await command.ExecuteNonQueryAsync();

                // Extract OUT parameters
                foreach (OracleParameter param in command.Parameters)
                {
                    if (param.Direction == ParameterDirection.Output ||
                        param.Direction == ParameterDirection.InputOutput)
                    {
                        outParameters[param.ParameterName] = param.Value;
                    }
                }

                _logger.LogInformation($"Successfully executed {procedureName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing stored procedure {procedureName}");
                throw;
            }

            return outParameters;
        }

        /// <summary>
        /// Creates an Oracle parameter for REF CURSOR output
        /// </summary>
        public static OracleParameter CreateRefCursorParameter(string parameterName)
        {
            return new OracleParameter(parameterName, OracleDbType.RefCursor, ParameterDirection.Output);
        }

        /// <summary>
        /// Creates an Oracle parameter for input
        /// </summary>
        public static OracleParameter CreateInputParameter(string parameterName, OracleDbType dbType, object? value)
        {
            return new OracleParameter(parameterName, dbType)
            {
                Value = value ?? DBNull.Value,
                Direction = ParameterDirection.Input
            };
        }

        /// <summary>
        /// Creates an Oracle parameter for output
        /// </summary>
        public static OracleParameter CreateOutputParameter(string parameterName, OracleDbType dbType, int size = 0)
        {
            var param = new OracleParameter(parameterName, dbType)
            {
                Direction = ParameterDirection.Output
            };

            if (size > 0)
            {
                param.Size = size;
            }

            return param;
        }
    }
}
