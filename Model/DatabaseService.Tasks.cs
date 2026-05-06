using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Space
{
    public static partial class DatabaseService
    {
        // ── Employee task view ────────────────────────────────────────────────

        public static async Task<List<TaskItem>> GetMyTasksAsync(int employeeId, string role)
        {
            var list = new List<TaskItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                var filter = role == "tester" ? "AND t.status='testing'" : "AND t.status<>'closed'";
                using (var cmd = new NpgsqlCommand($@"
                    SELECT t.id, t.title, p.name, t.priority, t.status, t.deadline
                    FROM tasks t JOIN projects p ON p.id=t.project_id
                    WHERE t.assignee_id=@eid {filter}
                    ORDER BY CASE t.priority WHEN 'critical' THEN 1 WHEN 'high' THEN 2
                        WHEN 'medium' THEN 3 ELSE 4 END, t.deadline ASC NULLS LAST", conn))
                {
                    cmd.Parameters.AddWithValue("eid", employeeId);
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync())
                            list.Add(new TaskItem
                            {
                                Id          = r.GetInt32(0),
                                Title       = r.GetString(1),
                                ProjectName = r.GetString(2),
                                Priority    = r.IsDBNull(3) ? "low"  : r.GetString(3),
                                Status      = r.IsDBNull(4) ? "open" : r.GetString(4),
                                Deadline    = r.IsDBNull(5) ? (DateTime?)null : r.GetDateTime(5)
                            });
                }
            }
            return list;
        }

        public static async Task AdvanceTaskStatusAsync(int taskId, string currentStatus)
        {
            var next = currentStatus == "open"        ? "in_progress"
                     : currentStatus == "in_progress" ? "testing" : "closed";
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("UPDATE tasks SET status=@s WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("s",  next);
                    cmd.Parameters.AddWithValue("id", taskId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("tasks");
        }

        public static async Task<List<LogItem>> GetTaskLogsAsync(int taskId)
        {
            var list = new List<LogItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT w.comment, w.log_date, w.hours_spent
                    FROM worklogs w WHERE w.task_id=@tid ORDER BY w.log_date DESC", conn))
                {
                    cmd.Parameters.AddWithValue("tid", taskId);
                    using (var r = await cmd.ExecuteReaderAsync())
                        while (await r.ReadAsync())
                            list.Add(new LogItem
                            {
                                Comment = r.IsDBNull(0) ? "" : r.GetString(0),
                                LogDate = r.GetDateTime(1).ToString("dd.MM.yyyy"),
                                Hours   = r.GetDecimal(2)
                            });
                }
            }
            return list;
        }

        // ── Task management (admin) ───────────────────────────────────────────

        public static async Task<List<TaskManageItem>> GetAllTasksManageAsync()
        {
            lock (_cacheLock)
            {
                if (IsCacheValid(_tasksCacheTime) && _tasksCache != null)
                    return new List<TaskManageItem>(_tasksCache);
            }

            var list = new List<TaskManageItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT t.id, t.title,
                           COALESCE(p.name,'—'), COALESCE(e.full_name,'—'),
                           COALESCE(t.priority,'low'), COALESCE(t.status,'open'),
                           t.deadline, t.project_id, t.assignee_id
                    FROM tasks t
                    LEFT JOIN projects p ON p.id = t.project_id
                    LEFT JOIN employees e ON e.id = t.assignee_id
                    ORDER BY t.id ASC", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new TaskManageItem
                        {
                            Id         = r.GetInt32(0),
                            Title      = r.GetString(1),
                            Project    = r.GetString(2),
                            Assignee   = r.GetString(3),
                            Priority   = r.GetString(4),
                            Status     = r.GetString(5),
                            Deadline   = r.IsDBNull(6) ? "—" : r.GetDateTime(6).ToString("dd.MM.yyyy"),
                            ProjectId  = r.IsDBNull(7) ? (int?)null : r.GetInt32(7),
                            AssigneeId = r.IsDBNull(8) ? (int?)null : r.GetInt32(8)
                        });
            }

            lock (_cacheLock)
            {
                _tasksCache     = new List<TaskManageItem>(list);
                _tasksCacheTime = DateTime.Now;
            }
            return list;
        }

        public static async Task AddTaskManageAsync(string title, int projectId, int? assigneeId,
            string priority, string status, DateTime? deadline)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO tasks (title, project_id, assignee_id, priority, status, deadline)
                    VALUES (@t,@p,@a,@pr,@s,@d)", conn))
                {
                    cmd.Parameters.AddWithValue("t",  title);
                    cmd.Parameters.AddWithValue("p",  projectId);
                    cmd.Parameters.AddWithValue("a",  assigneeId.HasValue ? (object)assigneeId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("pr", priority);
                    cmd.Parameters.AddWithValue("s",  status);
                    cmd.Parameters.AddWithValue("d",  deadline.HasValue ? (object)deadline.Value.Date : DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("tasks");
        }

        public static async Task UpdateTaskManageAsync(int id, string title, int projectId,
            int? assigneeId, string priority, string status, DateTime? deadline)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    UPDATE tasks SET title=@t, project_id=@p, assignee_id=@a,
                        priority=@pr, status=@s, deadline=@d
                    WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("t",  title);
                    cmd.Parameters.AddWithValue("p",  projectId);
                    cmd.Parameters.AddWithValue("a",  assigneeId.HasValue ? (object)assigneeId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("pr", priority);
                    cmd.Parameters.AddWithValue("s",  status);
                    cmd.Parameters.AddWithValue("d",  deadline.HasValue ? (object)deadline.Value.Date : DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("tasks");
        }

        public static async Task DeleteTaskManageAsync(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("DELETE FROM worklogs WHERE task_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
                using (var cmd = new NpgsqlCommand("DELETE FROM tasks WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("tasks");
        }
    }
}
