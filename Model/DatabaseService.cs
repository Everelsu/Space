using System;
using System.Collections.Generic;
using Npgsql;

namespace Space
{
    public static partial class DatabaseService
    {
        private const string ConnString =
            "Host=aws-1-ap-northeast-2.pooler.supabase.com;" +
            "Port=6543;" +
            "Database=postgres;" +
            "Username=postgres.ngvycmxfxipceazxgxnz;" +
            "Password=d2KkcSEzqzx3yaqv;" +
            "SSL Mode=Require;" +
            "Trust Server Certificate=true;" +
            "Pooling=true;" +
            "No Reset On Close=true;";

        private static readonly object _cacheLock = new object();
        private static List<ProjectItem>     _projectsCache;
        private static List<EmployeeItem>    _employeesCache;
        private static List<TeamItem>        _teamsCache;
        private static List<WorkLogItem>     _workLogsCache;
        private static List<TaskManageItem>  _tasksCache;
        private static List<DropdownItem>    _tasksDropdownCache;
        private static List<DropdownItem>    _employeesDropdownCache;
        private static List<DropdownItem>    _teamsDropdownCache;
        private static List<DropdownItem>    _projectsDropdownCache;
        private static DateTime?             _projectsCacheTime;
        private static DateTime?             _employeesCacheTime;
        private static DateTime?             _teamsCacheTime;
        private static DateTime?             _workLogsCacheTime;
        private static DateTime?             _tasksCacheTime;
        private static readonly TimeSpan     CacheDuration = TimeSpan.FromMinutes(5);

        public static NpgsqlConnection GetConnection() => new NpgsqlConnection(ConnString);

        private static bool IsCacheValid(DateTime? cacheTime)
            => cacheTime.HasValue && DateTime.Now - cacheTime.Value < CacheDuration;

        public static void InvalidateCache(string type = null)
        {
            lock (_cacheLock)
            {
                if (type == null || type == "projects")  { _projectsCache  = null; _projectsCacheTime  = null; }
                if (type == null || type == "employees") { _employeesCache = null; _employeesCacheTime = null; }
                if (type == null || type == "teams")     { _teamsCache     = null; _teamsCacheTime     = null; }
                if (type == null || type == "worklogs")  { _workLogsCache  = null; _workLogsCacheTime  = null; }
                if (type == null || type == "tasks")     { _tasksCache     = null; _tasksCacheTime     = null; }
                if (type == null || type == "dropdowns")
                {
                    _tasksDropdownCache     = null;
                    _employeesDropdownCache = null;
                    _teamsDropdownCache     = null;
                    _projectsDropdownCache  = null;
                }
            }
        }
    }
}
