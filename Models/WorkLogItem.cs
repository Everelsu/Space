namespace Space
{
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
}
