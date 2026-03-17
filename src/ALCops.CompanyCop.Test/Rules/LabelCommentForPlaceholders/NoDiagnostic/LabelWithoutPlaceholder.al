codeunit 50303 LabelNoPlaceholderTest
{
    procedure ShowMessage()
    var
        [||]GreetingLbl: Label 'Hello, welcome!';
    begin
        Message(GreetingLbl);
    end;
}
