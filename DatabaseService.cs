using System.Threading.Tasks;
using Npgsql;

namespace Space
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
    }

    public static class DatabaseService
    {
        private const string ConnString =
            "Host=aws-1-ap-northeast-2.pooler.supabase.com;" +
            "Port=6543;" +
            "Database=postgres;" +
            "Username=postgres.ngvycmxfxipceazxgxnz;" +
            "Password=d2KkcSEzqzx3yaqv;" +
            "SSL Mode=Require;" +
            "Trust Server Certificate=true;";

        public static NpgsqlConnection GetConnection()
            => new NpgsqlConnection(ConnString);

        public static async Task<bool> RegisterAsync(string username, string password, string fullName, string role)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();

                using (var check = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM users WHERE username = @u", conn))
                {
                    check.Parameters.AddWithValue("u", username);
                    var count = (long)await check.ExecuteScalarAsync();
                    if (count > 0) return false;
                }

                int userId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO users (username, password_hash, role) VALUES (@u, @p, @r) RETURNING id",
                    conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("p", password);
                    cmd.Parameters.AddWithValue("r", role);
                    userId = (int)await cmd.ExecuteScalarAsync();
                }

                using (var emp = new NpgsqlCommand(
                    "INSERT INTO employees (user_id, full_name) VALUES (@uid, @name)",
                    conn))
                {
                    emp.Parameters.AddWithValue("uid", userId);
                    emp.Parameters.AddWithValue("name", fullName);
                    await emp.ExecuteNonQueryAsync();
                }

                return true;
            }
        }

        public static async Task<UserInfo> LoginAsync(string username, string password)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();

                using (var cmd = new NpgsqlCommand(
                    "SELECT id, username, role FROM users WHERE username = @u AND password_hash = @p LIMIT 1",
                    conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("p", password);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new UserInfo
                            {
                                Id       = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Role     = reader.GetString(2)
                            };
                        }
                    }
                }
            }

            return null;
        }
    }
}
