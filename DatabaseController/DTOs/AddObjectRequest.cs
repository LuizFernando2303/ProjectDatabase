namespace DatabaseController.DTOs
{
    public class AddObjectRequest : Interfaces.IObject
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
