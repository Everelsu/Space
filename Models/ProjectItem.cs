namespace Space
{
    public class ProjectItem
    {
        public int    Id          { get; set; }
        public string Name        { get; set; }
        public string Description { get; set; }
        public string StartDate   { get; set; }
        public string Deadline    { get; set; }
        public string Status      { get; set; }
        public string Manager     { get; set; }

        public string StatusLabel => Status == "active" ? "Активен"
                                   : Status == "closed" ? "Закрыт" : Status;
    }
}
