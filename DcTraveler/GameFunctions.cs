using Dalamud;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Application.Network;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
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

        public int GetXLDcTravelerPort()
        {
            var port = 0;
            var gameWindow = GameWindow.Instance();
            var key = "XL.DcTraveler=";
            for (var i = 0UL; i < gameWindow->ArgumentCount; i++)
            {
                var arg = gameWindow->GetArgument(i);
                if (arg.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(arg.Substring(key.Length), out port);
                    break;
                }
            }
            if (port == 0)
                throw new Exception("Can not find port for XL DcTraveler");
            return port;
        }
    }
}
