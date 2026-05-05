namespace Space
{
    public class UserInfo
    {
        public int    Id         { get; set; }
        public string Username   { get; set; }
        public string Role       { get; set; }
        public int?   EmployeeId { get; set; }   // linked employee record (null if admin/no record)
    }
}
