using CQRS.Core.Domain;
using CQRS.Core.Handlers;
using CQRS.Core.Infrastructure;
using Post.Cmd.Domain.Aggregates;

namespace Post.Cmd.Infrastructure.Hadlers;

public class EventSourcingHandler : IEventSourcingHandler<PostAggregate>
{
    private readonly IEventStore _eventStore;

    public EventSourcingHandler(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<PostAggregate> GetByIdAsync(Guid aggregateId)
    {
        var aggregte = new PostAggregate();
        var events = await _eventStore.GetEventsAsync(aggregateId);

        if (events == null || !events.Any())
        {
            return aggregte;
        }

        aggregte.ReplayEvents(events);
        aggregte.Version = events.Select(x => x.Version).Max();

        return aggregte;
    }

    public async Task SaveAsync(AggregateRoot aggregate)
    {
        await _eventStore.SaveEventsAsync(aggregate.Id, aggregate.GetUncommitedChanges(), aggregate.Version);
        aggregate.MarkChangesAsCommitted();
    }
}
