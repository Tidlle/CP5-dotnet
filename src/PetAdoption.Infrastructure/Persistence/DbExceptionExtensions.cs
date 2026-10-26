using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PetAdoption.Infrastructure.Persistence;

internal static class DbExceptionExtensions
{
    // 2601: índice único duplicado | 2627: violação de UNIQUE/PK
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
