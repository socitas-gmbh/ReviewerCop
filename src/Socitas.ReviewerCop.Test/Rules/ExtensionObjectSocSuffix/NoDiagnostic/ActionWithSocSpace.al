page 50100 MyPage
{
    actions
    {
        area(Processing) { }
    }
}

pageextension 50100 MyPageExt extends MyPage
{
    actions
    {
        addlast(Processing)
        {
            action([||]"Open Service Codes SOC")
            {
                Caption = 'Open Service Codes';
            }
        }
    }
}
