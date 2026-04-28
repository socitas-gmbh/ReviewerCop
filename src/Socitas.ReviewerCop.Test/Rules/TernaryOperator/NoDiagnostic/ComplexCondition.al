// Complex condition (and/or) — no diagnostic for complex conditions
codeunit 50107 ComplexConditionTest
{
    procedure [||]Calculate(A: Boolean; B: Boolean; Val1: Decimal; Val2: Decimal): Decimal
    var
        Result: Decimal;
    begin
        if A and B then
            Result := Val1
        else
            Result := Val2;
        exit(Result);
    end;
}
