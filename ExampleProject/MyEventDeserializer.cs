using Confluent.Kafka;

namespace ExampleProject;

public class MyEventDeserializer : IDeserializer<MyEvent> {

    public MyEvent Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) {
        // Deserialize however you wish
        throw new NotImplementedException();
    }
}