// Public visibility is required by event-contract reflection checks.
#pragma warning disable CA1515
namespace Company.BoundedContext.DesignTests.Common.ToolkitEventFixtures
{
    public interface IDomainEventFixture
    {
        string FQDN { get; }
        DateTimeOffset OccurredOn { get; }
    }

    public abstract class ValidBaseDomainEventFixture(
        string aggregateName,
        string eventName,
        DateTimeOffset occurredOn
    ) : IDomainEventFixture
    {
        public string FQDN { get; } =
            $"Example.Company.{aggregateName}.{eventName}";
        public DateTimeOffset OccurredOn { get; } = occurredOn;
    }

    public abstract class FourComponentBaseDomainEventFixture(
        string companyName,
        string boundedContext,
        string aggregateName,
        string eventName,
        DateTimeOffset occurredOn
    ) : IDomainEventFixture
    {
        public string FQDN { get; } =
            $"{companyName}.{boundedContext}.{aggregateName}.{eventName}";
        public DateTimeOffset OccurredOn { get; } = occurredOn;
    }

    public abstract class InvalidBaseDomainEventFixture(
        string aggregateName,
        string eventName,
        DateTimeOffset occurredOn
    ) : IDomainEventFixture
    {
        public string FQDN { get; } =
            $"{eventName}.{aggregateName}.Example.Company";
        public DateTimeOffset OccurredOn { get; } = occurredOn;
    }
}

#pragma warning restore CA1515
