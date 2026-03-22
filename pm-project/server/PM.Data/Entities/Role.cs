namespace PM.Data.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public ICollection<UserDMO> Users { get; set; } = new List<UserDMO>();
    }
}