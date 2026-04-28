// Complex condition (and/or): diagnostic should NOT fire since only simple conditions are in scope
// This file intentionally has no diagnostic markers and is not referenced by a HasDiagnostic test case.
codeunit 50102 TernaryComplexConditionTest
{
    procedure Calculate(A: Boolean; B: Boolean; Val1: Decimal; Val2: Decimal): Decimal
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
