page 50200 "My Page"
{
    actions
    {
        area(Processing)
        {
            action(CarrierSetup)
            {
                Caption = 'Carrier Setup';
                Image = Setup;
                RunObject = page "Shipping Agents";
                ToolTip = 'Opens the shipping agents list.';
            }
        }
        area(Promoted)
        {
            actionref(CarrierSetup_Promoted; CarrierSetup) { }
        }
    }
}
