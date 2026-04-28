codeunit 50100 TernaryInAssignmentTest
{
    procedure Calculate(IsDiscount: Boolean; BaseAmount: Decimal): Decimal
    var
        Amount: Decimal;
    begin
        [|if|] IsDiscount then
            Amount := BaseAmount * 0.9
        else
            Amount := BaseAmount;
        exit(Amount);
    end;
}
