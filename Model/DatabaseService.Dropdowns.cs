using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace Space
{
    public static partial class DatabaseService
    {
        public static async Task<List<DropdownItem>> GetTasksDropdownAsync()
        {
            lock (_cacheLock)
            {
                if (IsCacheValid(_tasksCacheTime) && _tasksDropdownCache != null)
                    return new List<DropdownItem>(_tasksDropdownCache);
            }

            var list = new List<DropdownItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, title FROM tasks ORDER BY title", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new DropdownItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }

            lock (_cacheLock)
            {
                _tasksDropdownCache = new List<DropdownItem>(list);
                _tasksCacheTime     = DateTime.Now;
            }
            return list;
        }

        public static async Task<List<DropdownItem>> GetEmployeesDropdownAsync()
        {
            lock (_cacheLock)
            {
                if (IsCacheValid(_employeesCacheTime) && _employeesDropdownCache != null)
                    return new List<DropdownItem>(_employeesDropdownCache);
            }

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

            lock (_cacheLock)
            {
                _employeesDropdownCache = new List<DropdownItem>(list);
                _employeesCacheTime     = DateTime.Now;
            }
            return list;
        }

        public static async Task<List<DropdownItem>> GetTeamsDropdownAsync()
        {
            lock (_cacheLock)
            {
                if (IsCacheValid(_teamsCacheTime) && _teamsDropdownCache != null)
                    return new List<DropdownItem>(_teamsDropdownCache);
            }

            var list = new List<DropdownItem>();
            using (var conn = GetConnection())
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, name FROM teams ORDER BY name", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        list.Add(new DropdownItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }

            lock (_cacheLock)
            {
                _teamsDropdownCache = new List<DropdownItem>(list);
                _teamsCacheTime     = DateTime.Now;
            }
            return list;
        }

        public static async Task<List<DropdownItem>> GetProjectsDropdownAsync()
        {
            lock (_cacheLock)
            {
                if (IsCacheValid(_projectsCacheTime) && _projectsDropdownCache != null)
                    return new List<DropdownItem>(_projectsDropdownCache);
            }

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

            lock (_cacheLock)
            {
                _projectsDropdownCache = new List<DropdownItem>(list);
                _projectsCacheTime     = DateTime.Now;
            }
            return list;
        }
    }
}
