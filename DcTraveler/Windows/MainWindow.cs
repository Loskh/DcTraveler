using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using System;
using System.Numerics;

namespace DcTraveler.Windows;

public class MainWindow : Window, IDisposable
{
    private string GoatImagePath;
    private Plugin Plugin;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("My Amazing Window##With a hidden ID")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        GoatImagePath = goatImagePath;
        Plugin = plugin;
    }

    public void Dispose() { }

    public unsafe override void Draw()
    {
        var agentLobby = AgentLobby.Instance();
        var selectedCharacterContentId = agentLobby->SelectedCharacterContentId;
        var currentCharacterEntry = agentLobby->LobbyData.CharaSelectEntries[agentLobby->SelectedCharacterIndex];
        Util.ShowStruct(currentCharacterEntry, (ulong)currentCharacterEntry.Value);
        ImGui.Text($"{currentCharacterEntry.Value->LoginFlags}");
        //agentLobby->
        object addonObj = *agentLobby;
        //Util.ShowStruct(addonObj, (ulong)agentLobby);
        return;
    }
}
