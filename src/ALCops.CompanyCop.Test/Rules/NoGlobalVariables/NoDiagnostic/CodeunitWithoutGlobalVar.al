codeunit 50210 MyCleanCodeunit
{
    procedure [||]Increment(var Counter: Integer)
    begin
        Counter += 1;
    end;

    procedure GetGreeting(): Text
    var
        Greeting: Text;
    begin
        Greeting := 'Hello World';
        exit(Greeting);
    end;
}
