using Scanner.Models;
namespace Scanner.Models;

internal class ScanEvent<T>
{
    internal Event.Type Event { get; init; }
    internal T Data { get; init;}

    internal ScanEvent(Event.Type choiceEvent, T data)
    {
        Event = choiceEvent;
        Data = data;
    }
    internal static ScanEvent<T> Create(Event.Type choiceEvent, T data)
    {
        return new ScanEvent<T>(
            choiceEvent: choiceEvent,
            data: data
        );
    }
}