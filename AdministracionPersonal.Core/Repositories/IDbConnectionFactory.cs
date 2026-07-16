using System.Data;

namespace AdministracionPersonal.Core.Repositories;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
