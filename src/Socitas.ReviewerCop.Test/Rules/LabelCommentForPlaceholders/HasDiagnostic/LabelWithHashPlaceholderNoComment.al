codeunit 50301 LabelHashPlaceholderTest
{
    procedure ShowCount(Count: Integer)
    var
        CountMsgLbl: Label [|'Found #1 records.'|];
    begin
        Message(CountMsgLbl, Count);
    end;
}
