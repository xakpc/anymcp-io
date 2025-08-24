// ---
// id: ms-sql-mcp
// name: ms-sql-mcp.cs
// description: MCP server for Microsoft SQL Server; executes T-SQL and lists tables via a provided connection string.
// tags:
//     - database
// status: beta
// version: 1.0.0
// author: robalexclark
// license: MIT
// envVars:
//     - MSSQL_CONNECTION_STRING
//     - DatabaseList
// ---

#:package Microsoft.Data.SqlClient@6.1.1
#:package Microsoft.Extensions.Hosting@9.0.8
#:package ModelContextProtocol@0.3.0-preview.4

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Configure console logging with Trace level
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Retrieve connection string environment variable
string connectionString = Environment.GetEnvironmentVariable("MSSQL_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    await Console.Error.WriteLineAsync("Error: MSSQL_CONNECTION_STRING environment variable is not set.");
    Environment.Exit(1);
    return;
}

// Register the IDbConnectionFactory for the single connection
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

// Register MCP server and tools (instance-based)
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Build the host
IHost host = builder.Build();

// Test the database connection before running the host
IDbConnectionFactory dbFactory = host.Services.GetRequiredService<IDbConnectionFactory>();
try
{
    bool isValid = await dbFactory.ValidateConnectionAsync();
    if (!isValid)
    {
        await Console.Error.WriteLineAsync("Database connection test failed.");
        Environment.Exit(1);
        return;
    }

    Console.WriteLine("Database connection test succeeded.");
}
catch (Exception dbEx)
{
    await Console.Error.WriteLineAsync($"Database connection test failed: {dbEx.Message}");
    Environment.Exit(1);
    return;
}


// Setup cancellation token for graceful shutdown (Ctrl+C or SIGTERM)
using CancellationTokenSource cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true; // Prevent the process from terminating immediately
    cts.Cancel();
};

try
{
    // Run the host with cancellation support
    await host.RunAsync(cts.Token);
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"Unhandled exception: {ex}");
    Environment.ExitCode = 1;
}

public class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required(ErrorMessage = "MSSQL_CONNECTION_STRING environment variable is required")]
    public string ConnectionString { get; set; } = string.Empty;
}

public class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string name, DatabaseOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("MSSQL_CONNECTION_STRING environment variable must be provided and cannot be empty");
        }

        return ValidateOptionsResult.Success;
    }
}

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);
}

public class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    private readonly string _connectionString = connectionString;

    public DbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        SqlConnection connection = (SqlConnection)CreateConnection();
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using SqlConnection connection = (SqlConnection)await CreateOpenConnectionAsync(cancellationToken);
            await using SqlCommand command = new SqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

[McpServerToolType]
public class SqlExecutionTool(IDbConnectionFactory connectionFactory, ILogger<SqlExecutionTool> logger)
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;
    private readonly ILogger<SqlExecutionTool> _logger = logger;

    // Regex to detect valid T-SQL keywords at the beginning of queries
    private static readonly Regex ValidTSqlStartPattern = new(
        @"^\s*(SELECT|INSERT|UPDATE|DELETE|WITH|CREATE|ALTER|DROP|GRANT|REVOKE|EXEC|EXECUTE|DECLARE|SET|USE|BACKUP|RESTORE|TRUNCATE|MERGE)\s+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [McpServerTool, Description(@"Execute T-SQL queries against the connected Microsoft SQL Server database.
    
IMPORTANT: This tool ONLY accepts valid T-SQL (Transact-SQL) syntax for Microsoft SQL Server.

Supported operations:
- SELECT statements for data retrieval
- INSERT, UPDATE, DELETE for data modification  
- CREATE, ALTER, DROP for schema changes
- WITH clauses for CTEs (Common Table Expressions)
- EXEC/EXECUTE for stored procedures
- And other valid T-SQL statements

Examples of valid T-SQL:
- SELECT * FROM Users WHERE Active = 1
- INSERT INTO Products (Name, Price) VALUES ('Widget', 19.99)
- UPDATE Customers SET Status = 'Active' WHERE ID = 123
- CREATE TABLE Orders (ID int PRIMARY KEY, CustomerID int)

The query parameter must contain ONLY the T-SQL statement - no explanations, markdown, or other text.")]
    public async Task<string> ExecuteSql(
        [Description(@"The T-SQL query to execute. Must be valid Microsoft SQL Server T-SQL syntax only.
        Examples: 'SELECT * FROM Users', 'INSERT INTO Products VALUES (1, ''Name'')', 'CREATE TABLE Test (ID int)'
        Do NOT include explanations, markdown formatting, or non-SQL text.")]
        string query, CancellationToken cancellationToken = default)
    {
        // Log the incoming query for debugging
        _logger.LogInformation("Received SQL execution request. Query length: {QueryLength} characters", query.Length);
        _logger.LogDebug("SQL Query received: {Query}", query);

        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Empty or null query received");
            return "Error: SQL query cannot be empty";
        }

        // Validate that the query looks like T-SQL
        string trimmedQuery = query.Trim();
        if (!ValidTSqlStartPattern.IsMatch(trimmedQuery))
        {
            _logger.LogWarning("Invalid T-SQL query received. Query does not start with valid T-SQL keyword: {QueryStart}",
                trimmedQuery.Length > 50 ? trimmedQuery[..50] + "..." : trimmedQuery);

            return @"Error: Invalid T-SQL syntax. This tool only accepts valid Microsoft SQL Server T-SQL statements.

Valid T-SQL statements must start with keywords like:
- SELECT (for data retrieval)
- INSERT, UPDATE, DELETE (for data modification)  
- CREATE, ALTER, DROP (for schema changes)
- WITH (for CTEs)
- EXEC/EXECUTE (for stored procedures)
- And other valid T-SQL keywords

Examples:
✓ SELECT * FROM Users
✓ INSERT INTO Products (Name) VALUES ('Test')
✓ CREATE TABLE Orders (ID int)

✗ Please show me all users
✗ Can you create a table for orders?
✗ ```sql SELECT * FROM Users```

Please provide only the T-SQL statement without explanations or formatting.";
        }

        try
        {
            _logger.LogInformation("Executing T-SQL query starting with: {QueryStart}",
                trimmedQuery.Length > 30 ? trimmedQuery[..30] + "..." : trimmedQuery);

            await using System.Data.Common.DbConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await using System.Data.Common.DbCommand command = connection.CreateCommand();
            command.CommandText = query;

            // Determine if this is a SELECT query or a command
            bool isSelectQuery = trimmedQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                               trimmedQuery.StartsWith("WITH", StringComparison.OrdinalIgnoreCase);

            if (isSelectQuery)
            {
                // Handle SELECT queries - return data
                await using System.Data.Common.DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                string result = await FormatQueryResults(reader, cancellationToken);
                _logger.LogInformation("SELECT query executed successfully");
                return result;
            }
            else
            {
                // Handle INSERT/UPDATE/DELETE/DDL - return affected rows
                int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                string result = $"Query executed successfully. Rows affected: {rowsAffected}";
                _logger.LogInformation("Non-SELECT query executed successfully. Rows affected: {RowsAffected}", rowsAffected);
                return result;
            }
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "SQL execution failed with database error: {ErrorMessage}", ex.Message);
            return $"SQL Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL execution failed with general error: {ErrorMessage}", ex.Message);
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool, Description("List all tables in all accessible databases with basic information.")]
    public async Task<string> ListTables(CancellationToken cancellationToken = default)
    {
        try
        {
            await using System.Data.Common.DbConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            // Get all non-system databases the user has access to
            List<string> databaseNames = new List<string>();
            string databaseListEnv = Environment.GetEnvironmentVariable("DatabaseList");

            await using (System.Data.Common.DbCommand dbCommand = connection.CreateCommand())
            {
                if (!String.IsNullOrEmpty(databaseListEnv))
                {
                    List<string> allowedDatabases = databaseListEnv.Split(',').Select(d => d.Trim()).ToList();
                    List<string> parameterNames = new List<string>();
                    for (int i = 0; i < allowedDatabases.Count; i++)
                    {
                        string paramName = $"@p{i}";
                        System.Data.Common.DbParameter parameter = dbCommand.CreateParameter();
                        parameter.ParameterName = paramName;
                        parameter.Value = allowedDatabases[i];
                        dbCommand.Parameters.Add(parameter);
                        parameterNames.Add(paramName);
                    }
                    dbCommand.CommandText = $"SELECT name FROM sys.databases WHERE database_id > 4 AND HAS_DBACCESS(name) = 1 AND name IN ({String.Join(",", parameterNames)}) ORDER BY name";
                }
                else
                {
                    dbCommand.CommandText = "SELECT name FROM sys.databases WHERE database_id > 4 AND HAS_DBACCESS(name) = 1 ORDER BY name";
                }

                await using System.Data.Common.DbDataReader dbReader = await dbCommand.ExecuteReaderAsync(cancellationToken);
                while (await dbReader.ReadAsync(cancellationToken))
                {
                    databaseNames.Add(dbReader.GetString(0));
                }
            }

            // Build a UNION ALL query across all databases with database context
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < databaseNames.Count; i++)
            {
                if (i > 0)
                {
                    sb.AppendLine(" UNION ALL ");
                }

                string dbName = databaseNames[i];
                string escapedDbName = dbName.Replace("]", "]]", StringComparison.Ordinal);
                sb.Append($@"SELECT '{dbName}' AS DatabaseName,
                                s.name AS SchemaName,
                                t.name AS TableName,
                                ISNULL(p.rows, 0) AS 'RowCount',
                                t.type_desc AS TableType
                            FROM [{escapedDbName}].sys.tables t
                            JOIN [{escapedDbName}].sys.schemas s ON t.schema_id = s.schema_id
                            LEFT JOIN (
                                SELECT object_id, SUM(rows) AS rows
                                FROM [{escapedDbName}].sys.partitions
                                WHERE index_id IN (0,1)
                                GROUP BY object_id
                            ) p ON t.object_id = p.object_id");
            }
            sb.AppendLine(" ORDER BY DatabaseName, SchemaName, TableName");

            await using System.Data.Common.DbCommand command = connection.CreateCommand();
            command.CommandText = sb.ToString();
            await using System.Data.Common.DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            return await FormatQueryResults(reader, cancellationToken);
        }
        catch (Exception ex)
        {
            return $"Error listing tables: {ex.Message}";
        }
    }

    private static async Task<string> FormatQueryResults(DbDataReader reader, CancellationToken cancellationToken)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();

        if (!reader.HasRows)
        {
            return "Query executed successfully. No rows returned.";
        }

        // Get column headers
        int columnCount = reader.FieldCount;
        string[] columnNames = new string[columnCount];
        string[] columnTypeNames = new string[columnCount];
        int[] columnWidths = new int[columnCount];

        for (int i = 0; i < columnCount; i++)
        {
            columnNames[i] = reader.GetName(i);
            // Provider-specific type name (e.g., geography, geometry, hierarchyid)
            try
            {
                columnTypeNames[i] = reader.GetDataTypeName(i);
            }
            catch
            {
                columnTypeNames[i] = string.Empty;
            }
            columnWidths[i] = Math.Max(columnNames[i].Length, 10); // Minimum width of 10
        }

        // Read all rows to determine column widths
        List<object[]> rows = new List<object[]>();
        while (await reader.ReadAsync(cancellationToken))
        {
            object[] row = new object[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                row[i] = await SafeGetDisplayValueAsync(reader, i, columnTypeNames[i], cancellationToken);
                int valueLength = row[i]?.ToString()?.Length ?? 4;
                columnWidths[i] = Math.Max(columnWidths[i], valueLength);
            }
            rows.Add(row);
        }

        // Build header
        result.Append(string.Join(" | ", columnNames.Select((name, i) => name.PadRight(columnWidths[i])))).Append("\n");
        result.Append(string.Join("-+-", columnWidths.Select(w => new string('-', w)))).Append("\n");

        // Build data rows
        foreach (object[] row in rows)
        {
            result.Append(string.Join(" | ", row.Select((value, i) =>
                (value.ToString() ?? "NULL").PadRight(columnWidths[i])))).Append("\n");
        }

        result.Append($"\n({rows.Count} row(s) returned)").Append("\n");

        return result.ToString();
    }

    private static async Task<object> SafeGetDisplayValueAsync(DbDataReader reader, int ordinal, string typeName, CancellationToken cancellationToken)
    {
        if (await reader.IsDBNullAsync(ordinal, cancellationToken))
        {
            return "NULL";
        }

        try
        {
            // Try normal retrieval first
            return reader.GetValue(ordinal);
        }
        catch (Exception ex) when (IsMissingSqlServerTypes(ex))
        {
            // Gracefully handle spatial/UDT values when Microsoft.SqlServer.Types isn't available
            string tn = (typeName ?? string.Empty).ToLowerInvariant();
            string hint = tn switch
            {
                "geography" => "Use STAsText(), AsTextZM(), or CAST to nvarchar in your SELECT.",
                "geometry" => "Use STAsText(), AsTextZM(), or CAST to nvarchar in your SELECT.",
                "hierarchyid" => "Use ToString() or CAST to nvarchar in your SELECT.",
                _ => "Use CAST/CONVERT or server-side functions to text."
            };
            return $"<{(string.IsNullOrEmpty(typeName) ? "UDT" : typeName)} value not displayed; install Microsoft.SqlServer.Types or {hint}>";
        }
        catch (Exception ex)
        {
            // As a last resort, avoid failing the whole query; show error per-cell
            return $"<error: {ex.Message}>";
        }
    }

    private static bool IsMissingSqlServerTypes(Exception ex)
    {
        // Walk inner exceptions to find the assembly load failure for Microsoft.SqlServer.Types
        for (Exception? e = ex; e != null; e = e.InnerException!)
        {
            if (e is System.IO.FileNotFoundException fnf)
            {
                string msg = fnf.Message ?? string.Empty;
                if (msg.Contains("Microsoft.SqlServer.Types", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
