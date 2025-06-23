using MediatR;

namespace Domain.Common
{
    public abstract class AggregateRoot : BaseEntity
    {
        protected AggregateRoot() : base() { }
        protected AggregateRoot(Guid id) : base(id) { }
    }
}