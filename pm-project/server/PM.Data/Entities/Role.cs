namespace PM.Data.Entities
{
    public class Role
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public ICollection<UserDMO> Users { get; set; } = new List<UserDMO>();
    }
}