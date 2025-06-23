namespace Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; internal set; }
        public DateTime CreatedAt { get; internal set; }
        public DateTime UpdatedAt { get; internal set; }

        protected BaseEntity()
        {
            Id = Guid.NewGuid();
        }

        protected BaseEntity(Guid id)
        {
            Id = id;
        }

        protected void SetCreationTimestamp(DateTime timestamp)
        {
            CreatedAt = timestamp;
            UpdatedAt = timestamp;
        }

        protected void UpdateTimestamp(DateTime timestamp)
        {
            UpdatedAt = timestamp;
        }
    }
}