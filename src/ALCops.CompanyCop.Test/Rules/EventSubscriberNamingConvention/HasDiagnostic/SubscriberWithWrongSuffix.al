codeunit 50001 MyEventPublisher
{
    [IntegrationEvent(false, false)]
    procedure OnAfterInsertEvent()
    begin
    end;
}

codeunit 50501 PurchaseSubscribers
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyEventPublisher, OnAfterInsertEvent, '', false, false)]
    local procedure [|HandleSomethingElse|]()
    begin
        // Does not end with event name 'OnAfterInsertEvent' - should be e.g. 'SetRefOrderTypeInitValueOnAfterInsertEvent'
    end;
}
