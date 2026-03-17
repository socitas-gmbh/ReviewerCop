codeunit 50302 LabelWithCommentTest
{
    procedure Greet(CustomerName: Text[100])
    var
        [||]WelcomeMsgLbl: Label 'Welcome, %1!', Comment = '%1 = Customer Name';
    begin
        Message(WelcomeMsgLbl, CustomerName);
    end;
}
