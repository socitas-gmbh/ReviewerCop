codeunit 50400 LabelCommentFixTest
{
    var
        WelcomeMsgLbl: Label 'Dear %1, your account is %2.', Comment = '%1 = CustomerName, %2 = AccountNo';

    procedure Notify(CustomerName: Text[100]; AccountNo: Code[20])
    begin
        Message(WelcomeMsgLbl, CustomerName, AccountNo);
    end;
}
