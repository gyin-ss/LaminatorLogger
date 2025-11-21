using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;

namespace GL240DataLogger
{
    internal class PostgresAccess
    {
        private readonly string _connectionString;

        public PostgresAccess(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Executes a query and returns the results as a DataTable
        public DataTable ExecuteQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            using var cmd = new NpgsqlCommand(sql, conn);
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }
            }
            using var adapter = new NpgsqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        // Executes a non-query (INSERT, UPDATE, DELETE)
        public int ExecuteNonQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            using var cmd = new NpgsqlCommand(sql, conn);
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
                }
            }
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        // Example: Get all rows from a table
        public DataTable GetAllRows(string tableName)
        {
            string sql = $"SELECT * FROM \"{tableName}\"";
            return ExecuteQuery(sql);
        }

        // Bulk insert a DataTable into a PostgreSQL table
        public void BulkInsert(string tableName, DataTable dataTable)
        {
            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();

                using var writer = conn.BeginBinaryImport(GetCopyCommand(tableName, dataTable));
                foreach (DataRow row in dataTable.Rows)
                {
                    writer.StartRow();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        writer.Write(row[col]);
                    }
                }
                writer.Complete();
            }
            catch (Exception ex)
            {
                ;
            }

        }

        // Helper to generate COPY command for bulk insert
        private string GetCopyCommand(string tableName, DataTable dataTable)
        {
            var columnNames = new List<string>();
            foreach (DataColumn col in dataTable.Columns)
            {
                columnNames.Add($"\"{col.ColumnName}\"");
            }
            string columns = string.Join(", ", columnNames);
            return $"COPY \"{tableName}\" ({columns}) FROM STDIN (FORMAT BINARY)";
        }
    }
}
