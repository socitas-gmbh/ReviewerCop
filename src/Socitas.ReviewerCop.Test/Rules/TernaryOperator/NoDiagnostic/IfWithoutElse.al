// if without else — cannot become a ternary
codeunit 50105 IfWithoutElseTest
{
    procedure [||]Cap(var Amount: Decimal)
    begin
        if Amount > 1000 then
            Amount := 1000;
    end;
}
