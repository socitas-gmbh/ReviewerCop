codeunit 50303 GlobalVarCodeunit
{
    procedure RunPage()
    begin
        Page.Run(50303);
    end;
}

page 50306 GlobalVariablePage
{
    layout
    {
        area(Content)
        {
            field(MyGlobalField; MyGlobalVar)
            {
                Caption = 'My Value';
                ToolTip = 'Specifies my value.';
            }
        }
    }

    var
        MyGlobalVar: [||]Text[100];
}
