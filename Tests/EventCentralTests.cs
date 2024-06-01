namespace Tests;

using EventCentral = Waldenware.Events.EventCentral;

public class EventCentralTests
{
    [Fact]
    public void TestSubscribe()
    {
        // Arrange
        var eventCentral = EventCentral.Default;
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
        var eventCentral = EventCentral.Default;
        eventCentral.UnsubscribeAll();
        Action<int> handler = _ => {};
        var unsubscriber = eventCentral.Subscribe<int>(handler);

        // Act
        unsubscriber.Unsubscribe();

        // Assert
        Assert.Empty(eventCentral._subscribers);
    }

    [Fact]
    public void TestUnsubscribeByDisposing()
    {
        // Arrange
        var eventCentral = EventCentral.Default;
        eventCentral.UnsubscribeAll();
        Action<int> handler = _ => {};
        var unsubscriber = eventCentral.Subscribe<int>(handler);

        // Act
        unsubscriber.Dispose();

        // Assert
        Assert.Empty(eventCentral._subscribers);
    }
    
    [Fact]
    public void TestPublish()
    {
        // Arrange
        var eventCentral = EventCentral.Default;
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
        var eventCentral = EventCentral.Default;
        eventCentral.UnsubscribeAll();
        eventCentral.Subscribe<int>(_ => {});

        // Act
        eventCentral.UnsubscribeAll();

        // Assert
        Assert.Empty(eventCentral._subscribers);
    }

    [Fact]
    public void TestUnsubscribeScope()
    {
        // Arrange
        var eventCentral = EventCentral.Default;
        eventCentral.UnsubscribeAll();
        int x = 0;
        void Scoped()
        {
            eventCentral.Subscribe<int>(i => x = i);
        }

        // Act
        Scoped();
        eventCentral.Publish(17);

        // Assert
        Assert.Equal(17, x);
    }
}