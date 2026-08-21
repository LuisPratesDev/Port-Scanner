using Scanner.Interfaces;

namespace Scanner.Models;

internal class ScanEvent
{
    internal Event.Type Type { get; init; }
    internal IResult Data { get; init; }

    internal ScanEvent(Event.Type choiceEvent, IResult data)
    {
        Type = choiceEvent;
        Data = data;
    }
    internal static ScanEvent Create(Event.Type choiceEvent, IResult data)
    {
        return new ScanEvent(
            choiceEvent: choiceEvent,
            data: data
        );
    }
}