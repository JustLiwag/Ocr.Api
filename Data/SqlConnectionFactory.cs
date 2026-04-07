using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Data.SqlClient;

namespace Ocr.Api.Data
{
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("OcrDb")
                ?? throw new InvalidOperationException("Connection string 'OcrDb' is not configured.");
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}