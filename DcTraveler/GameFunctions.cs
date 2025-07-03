using Dalamud;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Application.Network;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DcTraveler
{
    [StructLayout(LayoutKind.Explicit, Size = 0x158)]
    public struct LobbyUIClientExposed
    {
        [FieldOffset(0x18)]
        public nint Context;
        [FieldOffset(0x158)]
        public byte State;
    }
    internal unsafe class GameFunctions
    {
        private unsafe delegate void ReturnToTitleDelegate(AgentLobby* agentLobby);
        private unsafe delegate void ReleaseLobbyContextDelegate(NetworkModule* agentLobby);
        private unsafe ReturnToTitleDelegate returnToTitle;
        private unsafe ReleaseLobbyContextDelegate releaseLobbyContext;
        public unsafe GameFunctions()
        {
            var returnToTitleAddr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? C6 87 ?? ?? ?? ?? ?? 33 C0 ");
            this.returnToTitle = Marshal.GetDelegateForFunctionPointer<ReturnToTitleDelegate>(returnToTitleAddr);

            var releaseLobbyContextAddr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 85 ?? ?? ?? ?? 48 85 C0");
            this.releaseLobbyContext = Marshal.GetDelegateForFunctionPointer<ReleaseLobbyContextDelegate>(releaseLobbyContextAddr);


        }
        public void ReturnToTitle()
        {
            this.returnToTitle(AgentLobby.Instance());
            Log.Information("Return to title");
        }

        public void RefreshGameServer()
        {
            var framework = Framework.Instance();
            var networkModule = framework->GetNetworkModuleProxy()->NetworkModule;
            this.releaseLobbyContext(networkModule);
            var agentLobby = AgentLobby.Instance();
            var lobbyUIClient2 = (LobbyUIClientExposed*)Unsafe.AsPointer(ref agentLobby->LobbyData.LobbyUIClient);
            lobbyUIClient2->Context = 0;
            lobbyUIClient2->State = 0;
            Log.Information("Refresh Game host addresses");
        }

        public void ChangeDevTestSid(string sid)
        {
            var agentLobby = AgentLobby.Instance();
            agentLobby->UnkUtf8Strings[0].SetString(sid);
            Log.Information("Refresh Dev.TestSid");
        }
        public void ChangeGameServer(string lobbyHost, string saveDataHost, string gmServerHost)
        {
            //var lobbyAgent = Plugin.GameGui.GetAddonByName(name);
            var framework = Framework.Instance();
            var networkModule = framework->GetNetworkModuleProxy()->NetworkModule;
            networkModule->ActiveLobbyHost.SetString(lobbyHost);
            networkModule->LobbyHosts[0].SetString(lobbyHost);
            networkModule->SaveDataBankHost.SetString(saveDataHost);

            for (int i = 0; i < framework->DevConfig.ConfigCount; ++i)
            {
                var entry = framework->DevConfig.ConfigEntry[i];
                if (entry.Value.String == null) continue;
                string name = entry.Name.ToString();
                if (name == "GMServerHost")
                {
                    entry.Value.String->SetString(gmServerHost);
                }
                else if (name == "SaveDataBankHost")
                {
                    entry.Value.String->SetString(saveDataHost);
                }
                else if (name == "LobbyHost01")
                {
                    entry.Value.String->SetString(lobbyHost);
                }
            }
            Log.Information($"Change Game host addresses:LobbyHost:{lobbyHost},SaveDataBankHost:{saveDataHost},GmHost:{gmServerHost}");
        }

        public string GetGameArgument(string key)
        {
            if (!key.EndsWith("="))
            {
                key = key + "=";
            }
            var gameWindow = GameWindow.Instance();
            for (var i = 0UL; i < gameWindow->ArgumentCount; i++)
            {
                var arg = gameWindow->GetArgument(i);
                if (arg.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(key.Length);
                }
            }
            throw new Exception($"未能从游戏参数中获取{key}");
        }
        public unsafe void LoginInGame()
        {
            var ptr = Plugin.GameGui.GetAddonByName("_TitleMenu", 1);
            if (ptr == 0)
                return;
            var atkUnitBase = (AtkUnitBase*)ptr;
            var loginGameButton = atkUnitBase->GetComponentButtonById(4);
            var loginGameButtonEvent = loginGameButton->AtkResNode->AtkEventManager.Event;
            Plugin.Framework.RunOnFrameworkThread(() => atkUnitBase->ReceiveEvent(AtkEventType.ButtonClick, 1, loginGameButtonEvent));
        }
    }
}
