codeunit 50402 TypeNameTest
{
    procedure Calculate()
    var
        [|Integer|]: Integer;
        [|Boolean|]: Boolean;
        [|Text|]: Text[100];
    begin
        Integer := 42;
        Boolean := true;
        Text := 'hello';
    end;
}
