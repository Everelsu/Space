using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Space
{
    public static partial class DatabaseService
    {
        public static async Task<List<ProjectItem>> GetAllProjectsAsync()
        {
            lock (_cacheLock)
            {
                if (IsCacheValid(_projectsCacheTime) && _projectsCache != null)
                    return new List<ProjectItem>(_projectsCache);
            }

            var list = new List<ProjectItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT p.id, p.name, p.description, p.start_date, p.deadline, p.status,
                           COALESCE(e.full_name,'—'), p.manager_id
                    FROM projects p LEFT JOIN employees e ON e.id = p.manager_id
                    ORDER BY p.id ASC", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new ProjectItem
                        {
                            Id          = r.GetInt32(0),
                            Name        = r.GetString(1),
                            Description = r.IsDBNull(2) ? "" : r.GetString(2),
                            StartDate   = r.IsDBNull(3) ? "—" : r.GetDateTime(3).ToString("dd.MM.yyyy"),
                            Deadline    = r.IsDBNull(4) ? "—" : r.GetDateTime(4).ToString("dd.MM.yyyy"),
                            Status      = r.IsDBNull(5) ? "—" : r.GetString(5),
                            Manager     = r.GetString(6),
                            ManagerId   = r.IsDBNull(7) ? (int?)null : r.GetInt32(7)
                        });
            }

            lock (_cacheLock)
            {
                _projectsCache     = new List<ProjectItem>(list);
                _projectsCacheTime = DateTime.Now;
            }
            return list;
        }

        public static async Task AddProjectAsync(string name, string desc, DateTime start,
            DateTime deadline, string status, int? managerId = null)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO projects (name, description, start_date, deadline, status, manager_id) VALUES (@n,@d,@s,@dl,@st,@mid)", conn))
                {
                    cmd.Parameters.AddWithValue("n",   name);
                    cmd.Parameters.AddWithValue("d",   desc ?? "");
                    cmd.Parameters.AddWithValue("s",   start.Date);
                    cmd.Parameters.AddWithValue("dl",  deadline.Date);
                    cmd.Parameters.AddWithValue("st",  status);
                    cmd.Parameters.AddWithValue("mid", (object)managerId ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("projects");
        }

        public static async Task UpdateProjectAsync(int id, string name, DateTime start,
            DateTime deadline, string status, int? managerId = null)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "UPDATE projects SET name=@n, start_date=@s, deadline=@dl, status=@st, manager_id=@mid WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id",  id);
                    cmd.Parameters.AddWithValue("n",   name);
                    cmd.Parameters.AddWithValue("s",   start.Date);
                    cmd.Parameters.AddWithValue("dl",  deadline.Date);
                    cmd.Parameters.AddWithValue("st",  status);
                    cmd.Parameters.AddWithValue("mid", (object)managerId ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("projects");
        }

        public static async Task DeleteProjectAsync(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("DELETE FROM projects WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("projects");
        }
    }
}
