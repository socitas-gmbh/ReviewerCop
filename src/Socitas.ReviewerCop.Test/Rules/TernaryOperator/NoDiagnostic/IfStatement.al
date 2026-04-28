// Already uses ternary — no diagnostic
codeunit 50103 AlreadyTernaryTest
{
    procedure [||]Calculate(IsDiscount: Boolean; BaseAmount: Decimal): Decimal
    var
        Amount: Decimal;
    begin
        Amount := IsDiscount ? BaseAmount * 0.9 : BaseAmount;
        exit(Amount);
    end;
}
