codeunit 50102 MyCodeunit3
{
    procedure DoSomething()
    begin
        [|// todo: fix edge case|]
        Message('Hello');
    end;
}
