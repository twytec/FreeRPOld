namespace FreeRP.Server.Data
{
    public class TreeItemData(object self, string text, string icon, object? parent)
    {
        public object? Parent { get; set; } = parent;
        public object Self { get; set; } = self;
        public string Text { get; set; } = text;
        public string Icon { get; set; } = icon;
        public HashSet<TreeItemData> TreeItems { get; set; } = [];
    }
}
