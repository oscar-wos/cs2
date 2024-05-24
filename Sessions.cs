using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;

namespace Sessions;

public partial class Sessions : BasePlugin, IPluginConfig<CoreConfig>
{
    public CoreConfig Config { get; set; } = new();
    
    public void OnConfigParsed(CoreConfig config)
    {
        Config = config;

        _database = new DatabaseFactory(config).Database;
        _database.CreateTablesAsync();
    }

    public override void Load(bool hotReload)
    {
        string ip = _ip.GetPublicIp();
        ushort port = (ushort)ConVar.Find("hostport")!.GetPrimitiveValue<int>();
        _server = _database!.GetServerAsync(ip, port).GetAwaiter().GetResult();

        RegisterListener<Listeners.OnMapStart>(async mapName =>
            _server.Map = await _database.GetMapAsync(mapName)
        );

        RegisterListener<Listeners.OnClientAuthorized>((playerSlot, steamId) =>
        {
            string playerName = Utilities.GetPlayerFromSlot(playerSlot)!.PlayerName;
            
            OnPlayerConnect(playerSlot, steamId.SteamId64, NativeAPI.GetPlayerIpAddress(playerSlot).Split(":")[0]).GetAwaiter().GetResult();
            CheckAlias(playerSlot, playerName).GetAwaiter().GetResult();
        });

        RegisterListener<Listeners.OnClientDisconnect>(playerSlot =>
        {
            if (!_players.TryGetValue(playerSlot, out PlayerSQL? value))
                return;

            _database.UpdateSeen(value.Id);
            _players.Remove(playerSlot);
        });

        RegisterEventHandler<EventPlayerChat>((@event, info) =>
        {
            CCSPlayerController? playerController = Utilities.GetPlayerFromUserid(@event.Userid);
            
            if (playerController == null || !playerController.IsValid || playerController.IsBot
                || @event.Text == null || !_players.TryGetValue(playerController.Slot, out PlayerSQL? value)
                || value.Session == null || _server.Map == null)
                return HookResult.Continue;

            MessageType messageType = @event.Teamonly ? MessageType.TeamChat : MessageType.Chat;
            _database.InsertMessage(value.Session.Id, value.Id, _server.Map.Id, messageType, @event.Text);

            return HookResult.Continue;
        }, HookMode.Post);
        
        _timer = AddTimer(1.0f, Timer_Repeat, TimerFlags.REPEAT);
        
        if (!hotReload)
            return;
        
        _server.Map = _database.GetMapAsync(Server.MapName).GetAwaiter().GetResult();

        Utilities.GetPlayers().Where(player => player.IsValid && !player.IsBot).ToList().ForEach(player => {
            OnPlayerConnect(player.Slot, player.SteamID, NativeAPI.GetPlayerIpAddress(player.Slot).Split(":")[0]).GetAwaiter().GetResult();
            CheckAlias(player.Slot, player.PlayerName).GetAwaiter().GetResult();
        });
    }
    
    public void Timer_Repeat()
    {
        List<CCSPlayerController> playerControllers = Utilities.GetPlayers();

        PlayerSQL[] players = playerControllers.Where(player => _players.TryGetValue(player.Slot, out PlayerSQL? p)).Select(player => _players[player.Slot]).ToArray();
        int[] playerIds = players.Select(player => player.Id).ToArray();
        int[] sessionIds = playerControllers.Where(player => _players.TryGetValue(player.Slot, out PlayerSQL? p) && p.Session != null).Select(player => _players[player.Slot].Session!.Id).ToArray();
        
        _database.UpdateSessionsBulkAsync(playerIds, sessionIds);
    }

    public async Task OnPlayerConnect(int playerSlot, ulong steamId, string ip)
    {
        _players[playerSlot] = await _database.GetPlayerAsync(steamId);
        _players[playerSlot].Session = await _database.GetSessionAsync(_players[playerSlot].Id, _server!.Id, _server.Map!.Id, ip);
    }

    public async Task CheckAlias(int playerSlot, string alias)
    {
        if (!_players.TryGetValue(playerSlot, out PlayerSQL? player))
            return;
        
        AliasSQL? recentAlias = await _database.GetAliasAsync(player.Id);
        alias = Regex.Replace(alias, @"[0\\]x[\da-fA-F]{2}", string.Empty);

        if (recentAlias == null || recentAlias.Alias != alias)
            _database.InsertAlias(player.Session!.Id, player.Id, _server!.Id, _server.Map!.Id, alias);
    }
}