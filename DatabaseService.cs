using System;
using System.Collections.Generic;
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

    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ProjectName { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public DateTime? Deadline { get; set; }
        public string DeadlineText => Deadline.HasValue ? Deadline.Value.ToString("dd.MM.yyyy") : "—";
        public string NextStatusLabel
        {
            get
            {
                switch (Status)
                {
                    case "open":        return "Начать";
                    case "in_progress": return "На тест";
                    case "testing":     return "Закрыть";
                    default:            return "";
                }
            }
        }
        public bool CanAdvance => Status != "closed";
    }

    public class LogItem
    {
        public string TaskTitle { get; set; }
        public string Comment { get; set; }
        public string LogDate { get; set; }
        public decimal Hours { get; set; }
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

        public static async Task<UserInfo> LoginAsync(string username, string password)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, username, role FROM users WHERE username = @u AND password_hash = @p LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("p", password);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            return new UserInfo
                            {
                                Id       = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Role     = reader.GetString(2)
                            };
                    }
                }
            }
            return null;
        }

        public static async Task<int> GetEmployeeIdAsync(int userId)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id FROM employees WHERE user_id = @uid LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    var result = await cmd.ExecuteScalarAsync();
                    return result != null ? (int)result : -1;
                }
            }
        }

        public static async Task<List<TaskItem>> GetMyTasksAsync(int employeeId, string role)
        {
            var tasks = new List<TaskItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();

                var filter = role == "tester"
                    ? "AND t.status = 'testing'"
                    : "AND t.status <> 'closed'";

                var sql = $@"
                    SELECT t.id, t.title, p.name, t.priority, t.status, t.deadline
                    FROM tasks t
                    JOIN projects p ON p.id = t.project_id
                    WHERE t.assignee_id = @eid {filter}
                    ORDER BY
                        CASE t.priority
                            WHEN 'critical' THEN 1
                            WHEN 'high'     THEN 2
                            WHEN 'medium'   THEN 3
                            ELSE 4
                        END, t.deadline ASC NULLS LAST";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("eid", employeeId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tasks.Add(new TaskItem
                            {
                                Id          = reader.GetInt32(0),
                                Title       = reader.GetString(1),
                                ProjectName = reader.GetString(2),
                                Priority    = reader.IsDBNull(3) ? "low" : reader.GetString(3),
                                Status      = reader.IsDBNull(4) ? "open" : reader.GetString(4),
                                Deadline    = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }
            return tasks;
        }

        public static async Task AdvanceTaskStatusAsync(int taskId, string currentStatus)
        {
            var next = currentStatus == "open" ? "in_progress"
                     : currentStatus == "in_progress" ? "testing"
                     : "closed";

            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "UPDATE tasks SET status = @s WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("s", next);
                    cmd.Parameters.AddWithValue("id", taskId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task<List<LogItem>> GetMyLogsAsync(int employeeId)
        {
            var logs = new List<LogItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT t.title, w.comment, w.log_date, w.hours_spent
                    FROM worklogs w
                    JOIN tasks t ON t.id = w.task_id
                    WHERE w.employee_id = @eid
                    ORDER BY w.log_date DESC
                    LIMIT 50", conn))
                {
                    cmd.Parameters.AddWithValue("eid", employeeId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            logs.Add(new LogItem
                            {
                                TaskTitle = reader.GetString(0),
                                Comment   = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                LogDate   = reader.GetDateTime(2).ToString("dd.MM.yyyy"),
                                Hours     = reader.GetDecimal(3)
                            });
                        }
                    }
                }
            }
            return logs;
        }

        public static async Task<List<LogItem>> GetTaskLogsAsync(int taskId)
        {
            var logs = new List<LogItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT w.comment, w.log_date, w.hours_spent
                    FROM worklogs w
                    WHERE w.task_id = @tid
                    ORDER BY w.log_date DESC", conn))
                {
                    cmd.Parameters.AddWithValue("tid", taskId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            logs.Add(new LogItem
                            {
                                Comment = reader.IsDBNull(0) ? "" : reader.GetString(0),
                                LogDate = reader.GetDateTime(1).ToString("dd.MM.yyyy"),
                                Hours   = reader.GetDecimal(2)
                            });
                        }
                    }
                }
            }
            return logs;
        }

        public static async Task AddWorkLogAsync(int taskId, int employeeId, decimal hours, string comment,
            DateTime? logDate = null)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO worklogs (task_id, employee_id, log_date, hours_spent, comment) VALUES (@tid, @eid, @d, @h, @c)",
                    conn))
                {
                    cmd.Parameters.AddWithValue("tid", taskId);
                    cmd.Parameters.AddWithValue("eid", employeeId);
                    cmd.Parameters.AddWithValue("d", logDate?.Date ?? DateTime.Today);
                    cmd.Parameters.AddWithValue("h", hours);
                    cmd.Parameters.AddWithValue("c", comment ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
