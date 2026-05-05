namespace Space
{
    public class DropdownItem
    {
        public int    Id   { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }
}
