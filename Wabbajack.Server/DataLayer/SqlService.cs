using System.Data.SqlClient;
using System.Threading.Tasks;
using Wabbajack.BuildServer;
using Wabbajack.DTOs.JsonConverters;
using Wabbajack.Server.DTOs;

namespace Wabbajack.Server.DataLayer
{
    public partial class SqlService
    {
        private AppSettings _settings;
        private Task<BunnyCdnFtpInfo> _mirrorCreds;
        private readonly DTOSerializer _dtos;

        public SqlService(AppSettings settings, DTOSerializer dtos)
        {
            _settings = settings;
            _mirrorCreds = BunnyCdnFtpInfo.GetCreds(StorageSpace.Mirrors);
            _dtos = dtos;

        }

        public async Task<SqlConnection> Open()
        {
            var conn = new SqlConnection(_settings.SqlConnection);
            await conn.OpenAsync();
            return conn;
        }
    }
}
