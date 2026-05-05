namespace Space
{
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

        public string NextStatusLabel => Status == "open"        ? "Начать"
                                       : Status == "in_progress" ? "На тест"
                                       : Status == "testing"     ? "Закрыть" : "";
        public bool CanAdvance       => Status != "closed";
        public bool CanAdvanceTester => Status == "testing";   // tester may only close tested tasks

        public string StatusLabel   => Status   == "open"        ? "Открыта"
                                     : Status   == "in_progress" ? "В работе"
                                     : Status   == "testing"     ? "Тест"
                                     : Status   == "closed"      ? "Закрыта"  : Status;

        public string PriorityLabel => Priority == "critical"    ? "Критичный"
                                     : Priority == "high"        ? "Высокий"
                                     : Priority == "medium"      ? "Средний"
                                     : Priority == "low"         ? "Низкий"   : Priority;
    }
}
