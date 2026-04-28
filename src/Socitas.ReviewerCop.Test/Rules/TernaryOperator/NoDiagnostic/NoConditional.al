// No if statement at all
codeunit 50104 NoConditionalTest
{
    procedure [||]Add(A: Decimal; B: Decimal): Decimal
    begin
        exit(A + B);
    end;
}
