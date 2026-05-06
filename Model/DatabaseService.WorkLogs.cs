using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Space
{
    public static partial class DatabaseService
    {
        public static async Task<List<WorkLogItem>> GetAllWorkLogsAsync()
        {
            lock (_cacheLock)
            {
                if (IsCacheValid(_workLogsCacheTime) && _workLogsCache != null)
                    return new List<WorkLogItem>(_workLogsCache);
            }

            var list = new List<WorkLogItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT w.id, t.title, e.full_name, w.log_date, w.hours_spent,
                           COALESCE(w.comment,''), w.task_id, w.employee_id
                    FROM worklogs w
                    JOIN tasks t ON t.id = w.task_id
                    JOIN employees e ON e.id = w.employee_id
                    ORDER BY w.id ASC", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new WorkLogItem
                        {
                            Id         = r.GetInt32(0),
                            Task       = r.GetString(1),
                            Employee   = r.GetString(2),
                            LogDate    = r.GetDateTime(3).ToString("dd.MM.yyyy"),
                            Hours      = r.GetDecimal(4),
                            Comment    = r.GetString(5),
                            TaskId     = r.GetInt32(6),
                            EmployeeId = r.GetInt32(7)
                        });
            }

            lock (_cacheLock)
            {
                _workLogsCache     = new List<WorkLogItem>(list);
                _workLogsCacheTime = DateTime.Now;
            }
            return list;
        }

        public static async Task AddWorkLogAsync(int taskId, int employeeId, decimal hours,
            string comment, DateTime? logDate = null)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO worklogs (task_id,employee_id,log_date,hours_spent,comment)
                    VALUES (@tid,@eid,@d,@h,@c)", conn))
                {
                    cmd.Parameters.AddWithValue("tid", taskId);
                    cmd.Parameters.AddWithValue("eid", employeeId);
                    cmd.Parameters.AddWithValue("d",   logDate?.Date ?? DateTime.Today);
                    cmd.Parameters.AddWithValue("h",   hours);
                    cmd.Parameters.AddWithValue("c",   comment ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("worklogs");
        }

        public static async Task UpdateWorkLogAsync(int id, decimal hours, string comment, DateTime date)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "UPDATE worklogs SET hours_spent=@h, comment=@c, log_date=@d WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("h",  hours);
                    cmd.Parameters.AddWithValue("c",  comment ?? "");
                    cmd.Parameters.AddWithValue("d",  date.Date);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("worklogs");
        }

        public static async Task DeleteWorkLogAsync(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("DELETE FROM worklogs WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            InvalidateCache("worklogs");
        }
    }
}
