using Oracle.ManagedDataAccess.Client;
using System;

namespace OracleConnectionTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Oracle Connection String Tester");
            Console.WriteLine("================================\n");

            // Try different connection string formats
            var connectionStrings = new[]
            {
                // Format 1: EZ Connect with default port and ORCL service
                "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=DbServer1:1521/ORCL",

                // Format 2: EZ Connect with XE service
                "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=DbServer1:1521/XE",

                // Format 3: EZ Connect with XEPDB1 (Oracle 18c+)
                "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=DbServer1:1521/XEPDB1",

                // Format 4: Using SID instead of Service Name
                "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=DbServer1)(PORT=1521))(CONNECT_DATA=(SID=ORCL)))",

                // Format 5: Full descriptor with SERVICE_NAME
                "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=DbServer1)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)))",

                // Format 6: TNS Alias (if configured in TNSNAMES.ORA)
                "User Id=e1241;Password=Pa$M0r$D16$#K;Data Source=DbServer1"
            };

            for (int i = 0; i < connectionStrings.Length; i++)
            {
                Console.WriteLine($"Test #{i + 1}:");
                Console.WriteLine($"Connection String: {MaskPassword(connectionStrings[i])}");

                try
                {
                    using (var connection = new OracleConnection(connectionStrings[i]))
                    {
                        connection.Open();
                        Console.WriteLine("✓ SUCCESS! This connection string works!");
                        Console.WriteLine($"Server Version: {connection.ServerVersion}");
                        Console.WriteLine($"Database Name: {connection.DatabaseName}");
                        Console.WriteLine("\n** USE THIS CONNECTION STRING IN appsettings.json **\n");
                        connection.Close();
                        return; // Exit after first successful connection
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ FAILED: {ex.Message}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("\n================================");
            Console.WriteLine("All connection attempts failed!");
            Console.WriteLine("\nPlease check:");
            Console.WriteLine("1. Is the Oracle database running?");
            Console.WriteLine("2. Can you ping DbServer1?");
            Console.WriteLine("3. Is the firewall blocking port 1521?");
            Console.WriteLine("4. Do you have the correct username/password?");
            Console.WriteLine("5. What is the exact hostname/IP and port?");
            Console.WriteLine("6. What is the service name or SID?");
        }

        static string MaskPassword(string connectionString)
        {
            var parts = connectionString.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = "Password=***";
                }
            }
            return string.Join(";", parts);
        }
    }
}
