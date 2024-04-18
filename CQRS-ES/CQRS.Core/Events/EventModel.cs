using MongoDB.Bson.Serialization.Attributes;

namespace CQRS.Core.Events;

[BsonDiscriminator("EventModel")]
public class EventModel
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string Id { get; set; }
    public DateTime TimeStamp { get; set; }
    [BsonElement("AggregateType")]
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    public Guid AggregateIdentifier { get; set; }
    public string AggregateType { get; set; }
    public int Version { get; set; }
    public string EventType { get; set; }
    public BaseEvent EventData { get; set; }
}
