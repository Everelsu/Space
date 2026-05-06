using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Space
{
    public static partial class DatabaseService
    {
        public static async Task<List<TeamItem>> GetAllTeamsAsync()
        {
            lock (_cacheLock)
            {
                if (IsCacheValid(_teamsCacheTime) && _teamsCache != null)
                    return new List<TeamItem>(_teamsCache);
            }

            var list = new List<TeamItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT t.id, t.name, COALESCE(e.full_name,'—'),
                           (SELECT COUNT(*) FROM employees WHERE team_id = t.id)
                    FROM teams t LEFT JOIN employees e ON e.id = t.lead_employee_id
                    ORDER BY t.id ASC", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new TeamItem
                        {
                            Id          = r.GetInt32(0),
                            Name        = r.GetString(1),
                            Lead        = r.GetString(2),
                            MemberCount = (int)(long)r.GetValue(3)
                        });
            }

            lock (_cacheLock)
            {
                _teamsCache     = new List<TeamItem>(list);
                _teamsCacheTime = DateTime.Now;
            }
            return list;
        }

        public static async Task AddTeamAsync(string name)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("INSERT INTO teams (name) VALUES (@n)", conn))
                {
                    cmd.Parameters.AddWithValue("n", name);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("teams");
        }

        public static async Task UpdateTeamAsync(int id, string name)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("UPDATE teams SET name=@n WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("n",  name);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("teams");
        }

        public static async Task DeleteTeamAsync(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("DELETE FROM teams WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("teams");
        }
    }
}
