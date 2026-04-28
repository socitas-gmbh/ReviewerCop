enum 50100 "My Status"
{
    Extensible = true;
    value(0; [||]"Open") { }
}

enumextension 50100 "My Status Ext" extends "My Status"
{
    value(50100; "Pending SOC") { }
}
