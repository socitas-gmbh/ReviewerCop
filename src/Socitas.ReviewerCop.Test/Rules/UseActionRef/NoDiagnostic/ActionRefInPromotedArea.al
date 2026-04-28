page 50200 "My Page"
{
    actions
    {
        area(Processing)
        {
            action([||]CarrierSetup)
            {
                Caption = 'Carrier Setup';
                ToolTip = 'Opens the shipping agents list.';
            }
        }
        area(Promoted)
        {
            actionref(CarrierSetup_Promoted; CarrierSetup) { }
        }
    }
}
