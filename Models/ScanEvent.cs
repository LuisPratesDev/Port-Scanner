namespace Scanner.Models;

internal class ScanEvent
{
    internal Event.Type Event { get; init; }
    internal Task Data { get; init;}

    internal ScanEvent(Event.Type choiceEvent, Task data)
    {
        Event = choiceEvent;
        Data = data;
    }
    internal static ScanEvent Create(Event.Type choiceEvent, Task data)
    {
        return new ScanEvent(
            choiceEvent: choiceEvent,
            data: data
        );
    }
}