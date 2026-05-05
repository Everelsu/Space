using System;

namespace Space
{
    public class TaskItem
    {
        public int       Id          { get; set; }
        public string    Title       { get; set; }
        public string    ProjectName { get; set; }
        public string    Priority    { get; set; }
        public string    Status      { get; set; }
        public DateTime? Deadline    { get; set; }

        public string DeadlineText    => Deadline.HasValue ? Deadline.Value.ToString("dd.MM.yyyy") : "—";
        public bool   CanAdvance      => Status != "closed";
        public string NextStatusLabel
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
    }
}
