namespace DatabaseController.Interfaces
{
    public interface IObject
    {
        string Guid { get; set; }
        string Name { get; set; }
        string Type { get; set; }
    }
}
