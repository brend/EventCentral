namespace Tests;

using EventCentral = Waldenware.Events.EventCentral;

public class EventCentralTests
{
    [Fact]
    public void TestSubscribe()
    {
        // Arrange
        var eventCentral = EventCentral.Instance;
        eventCentral.UnsubscribeAll();

        // Act
        eventCentral.Subscribe<int>(_ => {}, eventName: "TestEvent");

        // Assert
        Assert.NotNull(eventCentral._subscribers["TestEvent"]);
    }

    [Fact]
    public void TestUnsubscribe()
    {
        // Arrange
        var eventCentral = EventCentral.Instance;
        eventCentral.UnsubscribeAll();
        Action<int> handler = _ => {};
        eventCentral.Subscribe<int>(handler);

        // Act
        eventCentral.Unsubscribe(handler);

        // Assert
        Assert.Empty(eventCentral._subscribers);
    }
    
    [Fact]
    public void TestPublish()
    {
        // Arrange
        var eventCentral = EventCentral.Instance;
        eventCentral.UnsubscribeAll();
        int x = 0;
        eventCentral.Subscribe((int i) => x = i);

        // Act
        eventCentral.Publish(5);

        // Assert
        Assert.Equal(5, x);
    }

    [Fact]
    public void TestUnsubscribeAll()
    {
        // Arrange
        var eventCentral = EventCentral.Instance;
        eventCentral.UnsubscribeAll();
        eventCentral.Subscribe<int>(_ => {});

        // Act
        eventCentral.UnsubscribeAll();

        // Assert
        Assert.Empty(eventCentral._subscribers);
    }
}