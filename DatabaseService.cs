using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Space
{
    // ─── Models ───────────────────────────────────────────────────────────────

    public class UserInfo
    {
        public int    Id       { get; set; }
        public string Username { get; set; }
        public string Role     { get; set; }
    }

    public class ProjectItem
    {
        public int    Id          { get; set; }
        public string Name        { get; set; }
        public string Description { get; set; }
        public string StartDate   { get; set; }
        public string Deadline    { get; set; }
        public string Status      { get; set; }
        public string Manager     { get; set; }
    }

    public class EmployeeItem
    {
        public int    Id       { get; set; }
        public string FullName { get; set; }
        public string Email    { get; set; }
        public string Position { get; set; }
        public string Team     { get; set; }
        public string Username { get; set; }
        public string Role     { get; set; }
    }

    public class TeamItem
    {
        public int    Id          { get; set; }
        public string Name        { get; set; }
        public string Lead        { get; set; }
        public int    MemberCount { get; set; }
    }

    public class WorkLogItem
    {
        public int     Id         { get; set; }
        public string  Task       { get; set; }
        public string  Employee   { get; set; }
        public string  LogDate    { get; set; }
        public decimal Hours      { get; set; }
        public string  Comment    { get; set; }
        public int     TaskId     { get; set; }
        public int     EmployeeId { get; set; }
    }

    public class TaskItem
    {
        public int       Id              { get; set; }
        public string    Title           { get; set; }
        public string    ProjectName     { get; set; }
        public string    Priority        { get; set; }
        public string    Status          { get; set; }
        public DateTime? Deadline        { get; set; }
        public string    DeadlineText    => Deadline.HasValue ? Deadline.Value.ToString("dd.MM.yyyy") : "—";
        public string    NextStatusLabel
        {
            get
            {
                switch (Status)
                {
                    case "open":        return "Начать";
                    case "in_progress": return "На тест";
                    case "testing":     return "Закрыть";
                    default:            return "—";
                }
            }
        }
        public bool CanAdvance => Status != "closed";
    }

    public class LogItem
    {
        public string  TaskTitle { get; set; }
        public string  Comment   { get; set; }
        public string  LogDate   { get; set; }
        public decimal Hours     { get; set; }
    }

    public class TaskManageItem
    {
        public int    Id         { get; set; }
        public string Title      { get; set; }
        public string Project    { get; set; }
        public string Assignee   { get; set; }
        public string Priority   { get; set; }
        public string Status     { get; set; }
        public string Deadline   { get; set; }
        public int?   ProjectId  { get; set; }
        public int?   AssigneeId { get; set; }
    }

    public class DropdownItem
    {
        public int    Id   { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    // ─── Service ──────────────────────────────────────────────────────────────

    public static class DatabaseService
    {
        private const string ConnString =
            "Host=aws-1-ap-northeast-2.pooler.supabase.com;" +
            "Port=6543;" +
            "Database=postgres;" +
            "Username=postgres.ngvycmxfxipceazxgxnz;" +
            "Password=d2KkcSEzqzx3yaqv;" +
            "SSL Mode=Require;" +
            "Trust Server Certificate=true;" +
            "Pooling=false;" +
            "No Reset On Close=true;";

        public static NpgsqlConnection GetConnection() => new NpgsqlConnection(ConnString);

        // ── Auth ──────────────────────────────────────────────────────────────

        public static async Task<UserInfo> LoginAsync(string username, string password)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, username, role FROM users WHERE username=@u AND password_hash=@p LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("p", password);
                    using (var r = await cmd.ExecuteReaderAsync())
                        if (await r.ReadAsync())
                            return new UserInfo { Id = r.GetInt32(0), Username = r.GetString(1), Role = r.GetString(2) };
                }
            }
            return null;
        }

        // ── Projects ──────────────────────────────────────────────────────────

        public static async Task<List<ProjectItem>> GetAllProjectsAsync()
        {
            var list = new List<ProjectItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT p.id, p.name, p.description, p.start_date, p.deadline, p.status,
                           COALESCE(e.full_name,'—')
                    FROM projects p LEFT JOIN employees e ON e.id = p.manager_id
                    ORDER BY p.deadline", conn))
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
                            Manager     = r.GetString(6)
                        });
            }
            return list;
        }

        public static async Task AddProjectAsync(string name, string desc, DateTime start, DateTime deadline, string status)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO projects (name, description, start_date, deadline, status) VALUES (@n,@d,@s,@dl,@st)", conn))
                {
                    cmd.Parameters.AddWithValue("n",  name);
                    cmd.Parameters.AddWithValue("d",  desc ?? "");
                    cmd.Parameters.AddWithValue("s",  start.Date);
                    cmd.Parameters.AddWithValue("dl", deadline.Date);
                    cmd.Parameters.AddWithValue("st", status);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
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
        }

        // ── Employees ─────────────────────────────────────────────────────────

        public static async Task<List<EmployeeItem>> GetAllEmployeesAsync()
        {
            var list = new List<EmployeeItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT e.id, e.full_name, COALESCE(e.email,'—'), COALESCE(e.position,'—'),
                           COALESCE(t.name,'—'), COALESCE(u.username,'—'), COALESCE(u.role,'—')
                    FROM employees e
                    LEFT JOIN teams t ON t.id = e.team_id
                    LEFT JOIN users u ON u.id = e.user_id
                    ORDER BY e.full_name", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new EmployeeItem
                        {
                            Id       = r.GetInt32(0),
                            FullName = r.GetString(1),
                            Email    = r.GetString(2),
                            Position = r.GetString(3),
                            Team     = r.GetString(4),
                            Username = r.GetString(5),
                            Role     = r.GetString(6)
                        });
            }
            return list;
        }

        public static async Task AddEmployeeAsync(string fullName, string email, string position,
            string username, string password, string role)
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
                    "INSERT INTO employees (user_id,full_name,email,position) VALUES (@uid,@fn,@em,@pos)", conn))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.Parameters.AddWithValue("fn",  fullName);
                    cmd.Parameters.AddWithValue("em",  email ?? "");
                    cmd.Parameters.AddWithValue("pos", position ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task DeleteEmployeeAsync(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                // get user_id first
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
        }

        // ── Teams ─────────────────────────────────────────────────────────────

        public static async Task<List<TeamItem>> GetAllTeamsAsync()
        {
            var list = new List<TeamItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT t.id, t.name, COALESCE(e.full_name,'—'),
                           (SELECT COUNT(*) FROM employees WHERE team_id = t.id)
                    FROM teams t LEFT JOIN employees e ON e.id = t.lead_employee_id
                    ORDER BY t.name", conn))
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
        }

        // ── WorkLogs ──────────────────────────────────────────────────────────

        public static async Task<List<WorkLogItem>> GetAllWorkLogsAsync()
        {
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
                    ORDER BY w.log_date DESC", conn))
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
            return list;
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
        }

        // ── Tasks (for employee view) ─────────────────────────────────────────

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
            var next = currentStatus == "open" ? "in_progress"
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

        // ── Dropdowns ─────────────────────────────────────────────────────────

        public static async Task<List<DropdownItem>> GetTasksDropdownAsync()
        {
            var list = new List<DropdownItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, title FROM tasks WHERE status<>'closed' ORDER BY title", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new DropdownItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
            return list;
        }

        public static async Task<List<DropdownItem>> GetEmployeesDropdownAsync()
        {
            var list = new List<DropdownItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, full_name FROM employees ORDER BY full_name", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new DropdownItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
            return list;
        }

        public static async Task<List<DropdownItem>> GetProjectsDropdownAsync()
        {
            var list = new List<DropdownItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, name FROM projects ORDER BY name", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new DropdownItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
            return list;
        }

        // ── Tasks Management ──────────────────────────────────────────────────

        public static async Task<List<TaskManageItem>> GetAllTasksManageAsync()
        {
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
                    ORDER BY CASE t.priority WHEN 'critical' THEN 1 WHEN 'high' THEN 2
                        WHEN 'medium' THEN 3 ELSE 4 END, t.deadline ASC NULLS LAST", conn))
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
        }

        public static async Task DeleteTaskManageAsync(int id)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand("DELETE FROM tasks WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // ── Update methods ────────────────────────────────────────────────────

        public static async Task UpdateProjectAsync(int id, string name, DateTime start, DateTime deadline, string status)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "UPDATE projects SET name=@n, start_date=@s, deadline=@dl, status=@st WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("n",  name);
                    cmd.Parameters.AddWithValue("s",  start.Date);
                    cmd.Parameters.AddWithValue("dl", deadline.Date);
                    cmd.Parameters.AddWithValue("st", status);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task UpdateEmployeeAsync(int id, string fullName, string email, string position, string role)
        {
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "UPDATE employees SET full_name=@fn, email=@em, position=@pos WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id",  id);
                    cmd.Parameters.AddWithValue("fn",  fullName);
                    cmd.Parameters.AddWithValue("em",  email ?? "");
                    cmd.Parameters.AddWithValue("pos", position ?? "");
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
        }
    }
}
