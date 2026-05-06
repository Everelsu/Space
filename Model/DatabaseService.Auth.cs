using System.Threading.Tasks;
using Npgsql;

namespace Space
{
    public static partial class DatabaseService
    {
        public static async Task<UserInfo> LoginAsync(string username, string password)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT u.id, u.username, u.role, e.id
                    FROM users u
                    LEFT JOIN employees e ON e.user_id = u.id
                    WHERE u.username=@u AND u.password_hash=@p
                    LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("p", password);
                    using (var r = await cmd.ExecuteReaderAsync())
                        if (await r.ReadAsync())
                            return new UserInfo
                            {
                                Id         = r.GetInt32(0),
                                Username   = r.GetString(1),
                                Role       = r.GetString(2),
                                EmployeeId = r.IsDBNull(3) ? (int?)null : r.GetInt32(3)
                            };
                }
            }
            return null;
        }
    }
}
