namespace AiGatewayApi.Domain.Common;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

public abstract class BaseEntity : IAuditableEntity, ISoftDelete
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
}
