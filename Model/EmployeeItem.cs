namespace Space
{
    public class EmployeeItem
    {
        public int    Id       { get; set; }
        public string FullName { get; set; }
        public string Email    { get; set; }
        public string Position { get; set; }
        public int?   TeamId   { get; set; }
        public string Team     { get; set; }
        public string Username { get; set; }
        public string Role     { get; set; }

        public string RoleLabel => Role == "developer" ? "Разработчик"
                                 : Role == "tester"    ? "Тестировщик"
                                 : Role == "manager"   ? "Менеджер"
                                 : Role == "admin"     ? "Администратор" : Role;
    }
}
