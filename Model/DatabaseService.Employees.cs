using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Space
{
    public static partial class DatabaseService
    {
        public static async Task<List<EmployeeItem>> GetAllEmployeesAsync()
        {
            lock (_cacheLock)
            {
                if (IsCacheValid(_employeesCacheTime) && _employeesCache != null)
                    return new List<EmployeeItem>(_employeesCache);
            }

            var list = new List<EmployeeItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT e.id, e.full_name, COALESCE(e.email,'—'), COALESCE(e.position,'—'),
                           e.team_id, COALESCE(t.name,'—'), COALESCE(u.username,'—'), COALESCE(u.role,'—')
                    FROM employees e
                    LEFT JOIN teams t ON t.id = e.team_id
                    LEFT JOIN users u ON u.id = e.user_id
                    ORDER BY e.id ASC", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new EmployeeItem
                        {
                            Id       = r.GetInt32(0),
                            FullName = r.GetString(1),
                            Email    = r.GetString(2),
                            Position = r.GetString(3),
                            TeamId   = r.IsDBNull(4) ? (int?)null : r.GetInt32(4),
                            Team     = r.GetString(5),
                            Username = r.GetString(6),
                            Role     = r.GetString(7)
                        });
            }

            lock (_cacheLock)
            {
                _employeesCache     = new List<EmployeeItem>(list);
                _employeesCacheTime = DateTime.Now;
            }
            return list;
        }

        public static async Task AddEmployeeAsync(string fullName, string email, string position,
            string username, string password, string role, int? teamId = null)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                int userId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO users (username,password_hash,role) VALUES (@u,@p,@r) RETURNING id", conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("p", password);
                    cmd.Parameters.AddWithValue("r", role);
                    userId = (int)await cmd.ExecuteScalarAsync();
                }
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO employees (user_id,full_name,email,position,team_id) VALUES (@uid,@fn,@em,@pos,@tid)", conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.Parameters.AddWithValue("fn",  fullName);
                    cmd.Parameters.AddWithValue("em",  email ?? "");
                    cmd.Parameters.AddWithValue("pos", position ?? "");
                    cmd.Parameters.AddWithValue("tid", teamId.HasValue ? (object)teamId.Value : DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("employees");
        }

        public static async Task UpdateEmployeeAsync(int id, string fullName, string email,
            string position, string role, int? teamId = null)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "UPDATE employees SET full_name=@fn, email=@em, position=@pos, team_id=@tid WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id",  id);
                    cmd.Parameters.AddWithValue("fn",  fullName);
                    cmd.Parameters.AddWithValue("em",  email ?? "");
                    cmd.Parameters.AddWithValue("pos", position ?? "");
                    cmd.Parameters.AddWithValue("tid", teamId.HasValue ? (object)teamId.Value : DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
                using (var cmd = new NpgsqlCommand(
                    "UPDATE users SET role=@r WHERE id=(SELECT user_id FROM employees WHERE id=@eid)", conn))
                {
                    cmd.Parameters.AddWithValue("eid", id);
                    cmd.Parameters.AddWithValue("r",   role);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("employees");
        }

        public static async Task DeleteEmployeeAsync(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                int userId = -1;
                using (var cmd = new NpgsqlCommand("SELECT user_id FROM employees WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    var res = await cmd.ExecuteScalarAsync();
                    if (res != null && res != DBNull.Value) userId = (int)res;
                }
                using (var cmd = new NpgsqlCommand("DELETE FROM employees WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
                if (userId > 0)
                    using (var cmd = new NpgsqlCommand("DELETE FROM users WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", userId);
                        await cmd.ExecuteNonQueryAsync();
                    }
            }
            InvalidateCache("employees");
        }

        public static async Task<int> GetEmployeeIdAsync(int userId)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("SELECT id FROM employees WHERE user_id=@uid LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    var res = await cmd.ExecuteScalarAsync();
                    return res != null ? (int)res : -1;
                }
            }
        }
    }
}
