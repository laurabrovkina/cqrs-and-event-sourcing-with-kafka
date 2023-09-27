using CQRS.Core;
using CQRS.Core.Domain;
using CQRS.Core.Events;
using CQRS.Core.Exceptions;
using CQRS.Core.Infrastructure;
using CQRS.Core.Producers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Post.Cmd.Domain.Aggregates;
using Post.Cmd.Infrastructure.Config;

namespace Post.Cmd.Infrastructure.Stores;

public class EventStore : IEventStore
{
    private readonly IEventStoreRepository _eventStoreRepository;
    private readonly IEventProducer _eventProducer;
    private readonly IMongoClient _mongoClient;

    public EventStore(IEventStoreRepository eventStoreRepository, IEventProducer eventProducer, IOptions<MongoDbConfig> mongoDbConfig)
    {
        _eventStoreRepository = eventStoreRepository;
        _eventProducer = eventProducer;
        _mongoClient = new MongoClient(mongoDbConfig.Value.ConnectionString);
    }

    public async Task<List<BaseEvent>> GetEventsAsync(Guid aggregateId)
    {
        var eventStream = await _eventStoreRepository.FindByAggregateId(aggregateId);

        if (eventStream == null || !eventStream.Any())
        {
            throw new AggregateNotFoundException("Incorrect post Id provided!");
        }

        return eventStream.OrderBy(x => x.Version).Select(x => x.EventData).ToList();
    }

    public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<BaseEvent> events, int expectedVersion)
    {
        var eventStream = await _eventStoreRepository.FindByAggregateId(aggregateId);

        if (expectedVersion != -1 && eventStream[^1].Version != expectedVersion)
        {
            throw new ConcurrencyException();
        }

        var version = expectedVersion;
        var eventModels = new List<EventModel>();

        foreach (var @event in events)
        {
            version++;
            @event.Version = version;
            var eventType = @event.GetType().Name;
            var eventModel = new EventModel
            {
                TimeStamp = DateTime.Now,
                AggregateIdentifier = aggregateId,
                AggregateType = nameof(PostAggregate),
                Version = version,
                EventType = eventType,
                EventData = @event
            };

            eventModels.Add(eventModel);
        }

        using (var session = await _mongoClient.StartSessionAsync())
        {
            session.StartTransaction();

            try
            {
                foreach (var eventModel in eventModels)
                {
                    await _eventStoreRepository.SaveAsync(eventModel);

                    var topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC");
                    await _eventProducer.ProduceAsync(topic, eventModel.EventData); 
                }

                await session.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                throw new Exception("Error saving events to MongoDB", ex);
            }
        }
    }
}
