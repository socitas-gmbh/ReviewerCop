codeunit 50300 LabelPlaceholderTest
{
    procedure Greet(CustomerName: Text[100])
    var
        WelcomeMsgLbl: Label [|'Welcome, %1!'|];
    begin
        Message(WelcomeMsgLbl, CustomerName);
    end;
}
