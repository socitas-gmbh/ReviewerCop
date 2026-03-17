codeunit 50511 RegularProcedures
{
    procedure [||]ProcessOrder(OrderNo: Code[20])
    begin
        // Regular procedure without EventSubscriber attribute - naming not checked
        Message(OrderNo);
    end;

    local procedure ValidateAmount(Amount: Decimal): Boolean
    begin
        exit(Amount > 0);
    end;
}
