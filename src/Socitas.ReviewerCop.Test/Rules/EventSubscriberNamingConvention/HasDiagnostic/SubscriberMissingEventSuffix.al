codeunit 50000 MyPublisher
{
    [IntegrationEvent(false, false)]
    procedure OnAfterDoSomething()
    begin
    end;
}

codeunit 50500 SalesSubscribers
{
    [EventSubscriber(ObjectType::Codeunit, Codeunit::MyPublisher, OnAfterDoSomething, '', false, false)]
    local procedure [|HandleMyEvent|]()
    begin
        // Does not end with event name 'OnAfterDoSomething' - should be e.g. 'ProcessOrderOnAfterDoSomething'
    end;
}
