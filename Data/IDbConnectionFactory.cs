using Microsoft.Data.SqlClient;

namespace Ocr.Api.Data
{
    public interface IDbConnectionFactory
    {
        SqlConnection CreateConnection();
    }
}