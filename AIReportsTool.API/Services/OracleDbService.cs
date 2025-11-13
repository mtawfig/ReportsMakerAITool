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

                // Log column names for debugging
                var columnNames = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                _logger.LogInformation($"Successfully executed {procedureName}, returned {dataTable.Rows.Count} rows with columns: {columnNames}");
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

                // Extract OUT parameters and convert Oracle types
                foreach (OracleParameter param in command.Parameters)
                {
                    if (param.Direction == ParameterDirection.Output ||
                        param.Direction == ParameterDirection.InputOutput)
                    {
                        // Convert Oracle-specific types to standard .NET types
                        outParameters[param.ParameterName] = ConvertOracleType(param.Value);
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

        /// <summary>
        /// Safely gets a column value from a DataRow, handling missing columns and type conversions
        /// </summary>
        public static T? GetColumnValue<T>(DataRow row, string columnName, T? defaultValue = default)
        {
            try
            {
                if (!row.Table.Columns.Contains(columnName))
                {
                    return defaultValue;
                }

                var value = row[columnName];
                if (value == null || value == DBNull.Value)
                {
                    return defaultValue;
                }

                // Handle Oracle-specific types
                value = ConvertOracleType(value);

                // Handle type conversion properly
                if (typeof(T) == value.GetType())
                {
                    return (T)value;
                }

                // Use Convert.ChangeType for numeric and string conversions
                if (typeof(T).IsPrimitive || typeof(T) == typeof(decimal) || typeof(T) == typeof(string))
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }

                // Fallback to direct cast for other types
                return (T)value;
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                System.Diagnostics.Debug.WriteLine($"GetColumnValue failed for column '{columnName}': {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Converts Oracle-specific types to standard .NET types
        /// </summary>
        private static object ConvertOracleType(object value)
        {
            if (value == null || value == DBNull.Value)
                return value;

            var typeName = value.GetType().Name;

            // Handle Oracle-specific types
            switch (typeName)
            {
                case "OracleDecimal":
                    var oracleDecimal = (dynamic)value;
                    return oracleDecimal.IsNull ? (object)DBNull.Value : (decimal)oracleDecimal.Value;

                case "OracleString":
                    var oracleString = (dynamic)value;
                    return oracleString.IsNull ? (object)DBNull.Value : (string)oracleString.Value;

                case "OracleClob":
                    var oracleClob = (dynamic)value;
                    return oracleClob.IsNull ? (object)DBNull.Value : (string)oracleClob.Value;

                case "OracleDate":
                    var oracleDate = (dynamic)value;
                    return oracleDate.IsNull ? (object)DBNull.Value : (DateTime)oracleDate.Value;

                case "OracleTimeStamp":
                    var oracleTimeStamp = (dynamic)value;
                    return oracleTimeStamp.IsNull ? (object)DBNull.Value : (DateTime)oracleTimeStamp.Value;

                default:
                    return value;
            }
        }
    }
}
