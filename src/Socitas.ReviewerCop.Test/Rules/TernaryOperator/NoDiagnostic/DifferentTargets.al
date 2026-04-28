// Both branches assign but to different variables — not convertible
codeunit 50106 DifferentTargetsTest
{
    procedure [||]Route(IsA: Boolean; var X: Decimal; var Y: Decimal; Value: Decimal)
    begin
        if IsA then
            X := Value
        else
            Y := Value;
    end;
}
