using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DcTraveler.Windows;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
namespace DcTraveler;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IContextMenu ContextMenu { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDalamudPluginInterface DalamudPluginInterface { get; private set; } = null!;
    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("DcTraveler");
    //private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private WorldSelectorWindows WorldSelectorWindows { get; init; }
    private WaitingWindow WaitingWindow { get; init; }
    private static DcTravelClient DcTravelClient = null;
    internal SdoArea[] sdoAreas = null;
    internal static IFontHandle Font { get; private set; }

    internal static GameFunctions GameFunctions { get; private set; }
    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        SetupFont();
        //MainWindow = new MainWindow(this, goatImagePath);
        WorldSelectorWindows = new WorldSelectorWindows();
        WaitingWindow = new WaitingWindow();
        //WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(WorldSelectorWindows);
        WindowSystem.AddWindow(WaitingWindow);
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;


#if DEBUG
        ContextMenu.OnMenuOpened += this.OnContextMenuOpened;
#else
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "_CharaSelectListMenu", (type, args) => { ContextMenu.OnMenuOpened -= this.OnContextMenuOpened; });
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "_CharaSelectListMenu", (type, args) => { ContextMenu.OnMenuOpened += this.OnContextMenuOpened; });
#endif
        Task.Run(() => { this.sdoAreas = SdoArea.Get().Result; });
        //WaitingWindow.Open();
        GameFunctions = new GameFunctions();
        var port = GameFunctions.GetXLDcTravelerPort();
        DcTravelClient = new DcTravelClient(port);
    }

    public static void SetupFont()
    {
        Font?.Dispose();
        Font = PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(buildToolkit =>
        {
            buildToolkit.OnPreBuild(tk =>
            {
                var config = new SafeFontConfig { SizePx = 40 };
                var font = tk.AddDalamudAssetFont(DalamudAsset.NotoSansScMedium, config);
                config.MergeFont = font;
                tk.AddGameSymbol(config);
                tk.SetFontScaleMode(font, FontScaleMode.UndoGlobalScale);
            });
        });
    }

    private unsafe void OnContextMenuOpened(IMenuOpenedArgs args)
    {
        if (args.AddonPtr != 0 || args.MenuType != ContextMenuType.Default)
        {
            return;
        }
        var agentLobby = AgentLobby.Instance();
        var selectedCharacterContentId = agentLobby->SelectedCharacterContentId;
        var currentCharacterEntry = agentLobby->LobbyData.CharaSelectEntries[agentLobby->SelectedCharacterIndex].Value;
        var currentWorldId = currentCharacterEntry->CurrentWorldId;
        var homeWorldId = currentCharacterEntry->HomeWorldId;
        var isDcTravling = currentCharacterEntry->LoginFlags == CharaSelectCharacterEntryLoginFlags.DCTraveling || currentCharacterEntry->LoginFlags == CharaSelectCharacterEntryLoginFlags.DCTraveling;
        if (isDcTravling)
        {
            args.AddMenuItem(new MenuItem
            {
                Name = "超域返回",
                OnClicked = (clickedArgs) => TravelBack(homeWorldId, currentWorldId, selectedCharacterContentId),
                Prefix = Dalamud.Game.Text.SeIconChar.CrossWorld,
                PrefixColor = 48,
                IsEnabled = true
            });
        }
        else
        {
            args.AddMenuItem(new MenuItem
            {
                Name = "超域传送",
                //OnClicked = this.GetMenuItemClickedHandler(itemId),
                Prefix = Dalamud.Game.Text.SeIconChar.CrossWorld,
                PrefixColor = 48,
                IsEnabled = (currentWorldId == homeWorldId)
            });
        }
    }

    private void TravelOut()
    {

    }

    private void TravelBack(int homeWorldId, int currentWorldId, ulong contentId)
    {
        //return;
        //var areas = DcTravelClient.QueryGroupListTravelTarget();
        if (!DcTravelClient.IsValid)
        {
            Log.Error("Can not connect to XL");
            return;
        }
        var worldSheet = DataManager.GetExcelSheet<World>();
        var homeWorld = worldSheet.GetRow((uint)homeWorldId);
        var homeDcGroupName = homeWorld.DataCenter.Value.Name.ToString();
        var currentWorld = worldSheet.GetRow((uint)currentWorldId);
        var currentDcGroupName = currentWorld.DataCenter.Value.Name.ToString();
        var currentGroup = DcTravelClient.CachedAreas.First(x => x.AreaName == currentDcGroupName).GroupList.First(x => x.GroupCode == currentWorld.InternalName.ToString());
        MigrationOrder order = null;

        Task.Run(async () =>
        {
            order = GetTravelingOrder(contentId);
            Log.Information($"Find back order: {order.OrderId}");
            if (order != null)
            {
                await Framework.RunOnFrameworkThread(GameFunctions.ReturnToTitle);
                //return;
                //GameFunctions.ReturnToTitle();
                var orderId = await DcTravelClient.TravelBack(order.OrderId, currentGroup.GroupId, currentGroup.GroupCode, currentGroup.GroupName);
                Log.Information($"Get an order: {orderId}");
                WaitingWindow.Open();
                //WaitingWindow.Status = 
                while (true)
                {
                    var status = await DcTravelClient.QueryOrderStatus(orderId);
                    Log.Information($"Current status:{status}");
                    WaitingWindow.Status = status;
                    if (!(status == MigrationStatus.InPrepare || status == MigrationStatus.InQueue))
                    {
                        break;
                    }
                    await Task.Delay(2000);
                }
                WaitingWindow.IsOpen = false;
                var targetArea = this.sdoAreas.FirstOrDefault(x => x.AreaName == homeDcGroupName);
                GameFunctions.ChangeGameServer(targetArea.AreaLobby, targetArea.AreaConfigUpload, targetArea.AreaGm);
                GameFunctions.RefreshGameServer();
                var newTicket = await DcTravelClient.RefreshGameSessionId();
                GameFunctions.ChangeDevTestSid(newTicket);
                //WorldSelectorWindows.OpenTravelWindow(true, false, true, DcTravelClient.CachedAreas, currentDcGroupName, currentWorld.InternalName.ToString(), homeDcGroupName, homeWorld.InternalName.ToString());
            }
        });
    }
    private MigrationOrder GetTravelingOrder(ulong contentId)
    {
        var contentIdStr = contentId.ToString();
        var maxPageNum = 1;
        var currentPageNum = 1;
        while (true)
        {
            var orders = DcTravelClient.QueryMigrationOrders(currentPageNum).Result;
            var order = orders.Orders.First(x => x.Status == OrderStatus.Arrival && x.ContentId == contentIdStr);
            if (order == null)
            {
                maxPageNum = orders.TotalPageNum;
                currentPageNum++;
                if (currentPageNum > maxPageNum)
                {
                    Log.Error($"Fail to find order for {contentId}");
                    return null;
                }
            }
            else
            {
                return order;
            }
        }
    }
    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

#if DEBUG
        ContextMenu.OnMenuOpened -= this.OnContextMenuOpened;
#endif
        WorldSelectorWindows.Dispose();
        //MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    //public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
