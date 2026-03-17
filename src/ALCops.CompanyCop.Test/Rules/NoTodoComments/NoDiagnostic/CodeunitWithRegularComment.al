codeunit 50111 MyCodeunitWithComments
{
    procedure Calculate(Amount: Decimal): Decimal
    begin
        [||]// Multiply by tax rate
        exit(Amount * 1.19);
    end;

    procedure SendNotification()
    begin
        // Block comment: notify the user
        Message('Done');
    end;
}
