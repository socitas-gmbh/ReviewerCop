codeunit 50002 MyEventSource
{
    [IntegrationEvent(false, false)]
    procedure OnAfterProcess()
    begin
    end;

    [IntegrationEvent(false, false)]
    procedure OnBeforeInsertEvent()
    begin
    end;
}

codeunit 50510 CorrectSubscribers
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyEventSource, OnAfterProcess, '', false, false)]
    local procedure [||]HandleOnAfterProcess()
    begin
        // Ends with event name 'OnAfterProcess'
    end;

    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyEventSource, OnBeforeInsertEvent, '', false, false)]
    local procedure SetRefOrderTypeInitValueOnBeforeInsertEvent()
    begin
        // FunctionName+EventName pattern - ends with 'OnBeforeInsertEvent'
    end;
}
