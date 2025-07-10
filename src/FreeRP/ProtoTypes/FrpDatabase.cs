namespace FreeRP.Database
{
    public partial class FrpDatabase
    {
        public string Owner { get; set; } = string.Empty;

        public string GetName()
        {
            if (string.IsNullOrEmpty(Name))
                return DatabaseId;

            return Name;
        }
    }
}
