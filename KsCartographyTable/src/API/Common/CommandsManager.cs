using System;
using System.Collections.Generic;
using System.Reflection;
using Kaisentlaia.KsCartographyTableMod.API.Common;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using System.Linq;
using Vintagestory.API.Util;
using Vintagestory.API.Config;

namespace Kaisentlaia.KsCartographyTableMod.GameContent
{
    public class CommandsManager(ICoreAPI api)
    {
        private readonly ICoreAPI Api = api;

        private static readonly Dictionary<string, (string Name, string Description, string Unit)> SettingsMeta = new()
        {
            ["ImmersiveMode"]   = (
                Lang.Get(CartographyTableLangCodes.CONFIG_IMMERSIVE_MODE_NAME),
                Lang.Get(CartographyTableLangCodes.CONFIG_IMMERSIVE_MODE_COMMENT),
                ""
            ),
            ["ChunksPerPacket"] = (
                Lang.Get(CartographyTableLangCodes.CONFIG_CHUNKS_PER_PACKET_NAME),
                Lang.Get(CartographyTableLangCodes.CONFIG_CHUNKS_PER_PACKET_COMMENT),
                ""
            ),
            ["PacketDelay"]     = (
                Lang.Get(CartographyTableLangCodes.CONFIG_PACKET_DELAY_NAME),
                Lang.Get(CartographyTableLangCodes.CONFIG_PACKET_DELAY_COMMENT),
                "seconds"
            ),
            ["VerboseDebug"]    = (
                Lang.Get(CartographyTableLangCodes.CONFIG_VERBOSE_DEBUG_NAME),
                Lang.Get(CartographyTableLangCodes.CONFIG_VERBOSE_DEBUG_COMMENT),
                ""
            ),
            ["WaypointUpload"]  = (
                Lang.Get(CartographyTableLangCodes.CONFIG_WAYPOINT_UPLOAD_NAME),
                Lang.Get(CartographyTableLangCodes.CONFIG_WAYPOINT_UPLOAD_COMMENT),
                ""
            ),
            ["WaypointDownload"]= (
                Lang.Get(CartographyTableLangCodes.CONFIG_WAYPOINT_DOWNLOAD_NAME),
                Lang.Get(CartographyTableLangCodes.CONFIG_WAYPOINT_DOWNLOAD_COMMENT),
                ""
            ),
        };

        public void RegisterCommands()
        {
            CommandArgumentParsers parsers = Api.ChatCommands.Parsers;
            if (Api.Side == EnumAppSide.Server)
            {
                RegisterServerCommands(parsers);
            }
            else if (Api.Side == EnumAppSide.Client)
            {
                RegisterClientCommands(parsers);
            }
        }

        private void RegisterClientCommands(CommandArgumentParsers parsers)
        {
            IChatCommand kctCommand = Api.ChatCommands
                .Create("kct")
                .WithAlias("kscartographytable")
                .WithDescription("K's Cartography Table commands")
                .RequiresPlayer();

            RegisterConfigCommands(parsers, kctCommand);

            IChatCommand waypointsCommand = kctCommand
                .BeginSubCommand("waypoints")
                .WithAlias("wps")
                .WithDescription("Waypoint management commands");

            waypointsCommand
                .BeginSubCommand("wipe")
                .WithDescription("Deletes all waypoints from your map.<br><br>Running the command with 'maponly' will delete the waypoints from your map, but will let you get them back from the cartography table. Running it with 'mapandtable' will let you delete them also from the cartography table the next time you transcribe your waypoints on it.<br><br>Without the 'confirm' arg, does a dry-run only!")
                .RequiresPlayer()
                .WithArgs(parsers.WordRange("mode", ["maponly", "mapandtable"]), parsers.OptionalWordRange("confirm", ["confirm", "dryrun"]))
                .HandleWith((args) => {
                    bool confirmed = !args.Parsers[1].IsMissing && ((string)args.Parsers[1].GetValue()).Equals("confirm", StringComparison.OrdinalIgnoreCase);
                    bool mapOnly = !args.Parsers[0].IsMissing && ((string)args.Parsers[0].GetValue()).Equals("maponly", StringComparison.OrdinalIgnoreCase);

                    KsCartographyTableModSystem.ClientCartographyService.WipeWaypoints(!confirmed, mapOnly);

                    return TextCommandResult.Success();
                })
                .EndSubCommand();
        }

        private void RegisterServerCommands(CommandArgumentParsers parsers)
        {
            IChatCommand kctCommand = Api.ChatCommands
                .Create("kct")
                .WithAlias("kscartographytable")
                .WithDescription("K's Cartography Table commands")
                .RequiresPrivilege(Privilege.root);

            RegisterConfigCommands(parsers, kctCommand);

            IChatCommand waypointsCommand = kctCommand
                .BeginSubCommand("waypoints")
                .WithAlias("wps")
                .WithDescription("Waypoint management commands")
                .RequiresPrivilege(Privilege.root);

            waypointsCommand
                .BeginSubCommand("wipe")
                .WithDescription("Deletes all waypoints from the maps of all players. Use with caution.<br><br>Running the command with 'maponly' will delete the waypoints from all the player's maps, but will allow the players to get them back from the cartography table. Running it with 'mapandtable' will delete them also from the cartography table the next time a player transcribes their waypoints on it.<br><br Without the 'confirm' arg, does a dry-run only!")
                .RequiresPrivilege(Privilege.root)
                .WithArgs(parsers.WordRange("mode", ["maponly", "mapandtable"]), parsers.OptionalWordRange("confirm", ["confirm", "dryrun"]))
                .HandleWith((args) =>
                {
                    bool confirmed = !args.Parsers[1].IsMissing && ((string)args.Parsers[1].GetValue()).Equals("confirm", StringComparison.OrdinalIgnoreCase);
                    bool mapOnly = !args.Parsers[0].IsMissing && ((string)args.Parsers[0].GetValue()).Equals("maponly", StringComparison.OrdinalIgnoreCase);

                    TextCommandResult result = KsCartographyTableModSystem.ServerCartographyService.WipeWaypoints(!confirmed, null, mapOnly);

                    return result;
                })
                .EndSubCommand();
        }

        private void RegisterConfigCommands(CommandArgumentParsers parsers, IChatCommand kctCommand)
        {
            IChatCommand configCommands = kctCommand
                .BeginSubCommand("config")
                .WithDescription("Configuration commands");

            switch (Api.Side)
            {
                case EnumAppSide.Client:
                    configCommands.RequiresPlayer();
                    break;
                case EnumAppSide.Server:
                    configCommands.RequiresPrivilege(Privilege.root);
                    break;
            }

            IEnumerable<PropertyInfo> settings = typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.CanRead && p.CanWrite && p.PropertyType.IsValueType);

            string[] disallowedCommands = Api.Side == EnumAppSide.Server ? ["immersivemode", "waypointupload", "waypointdownload"] : [];

            settings.Foreach(setting =>
            {
                string cmdName = setting.Name.ToLowerInvariant();
                if (disallowedCommands.Contains(cmdName))
                {
                    return;
                }
                (string Name, string Description, string Unit) meta = SettingsMeta.GetValueOrDefault(setting.Name, (setting.Name, "", ""));
                Type type = setting.PropertyType;

                ICommandArgumentParser parser = type switch
                {
                    Type t when t == typeof(bool)   => parsers.OptionalBool("on/off"),
                    Type t when t == typeof(int)    => parsers.OptionalInt(string.IsNullOrEmpty(meta.Unit) ? "amount" : meta.Unit, (int)(setting.GetValue(null) ?? 0)),
                    Type t when t == typeof(double) => parsers.OptionalDouble(string.IsNullOrEmpty(meta.Unit) ? "amount" : meta.Unit, (double)(setting.GetValue(null) ?? 0.0)),
                    Type t when t == typeof(float)  => parsers.OptionalDouble(string.IsNullOrEmpty(meta.Unit) ? "amount" : meta.Unit, (double)(setting.GetValue(null) ?? 0.0)),
                    _ => throw new NotSupportedException($"Config type {type.Name} not supported")
                };

                IChatCommand subcommand = configCommands
                    .BeginSubCommand(cmdName)
                    .WithDescription(meta.Description)
                    .WithArgs(parser)
                    .HandleWith(args => HandleConfigCommand(args, setting, type, meta));

                switch (Api.Side)
                {
                    case EnumAppSide.Client:
                        subcommand.RequiresPlayer();
                        break;
                    case EnumAppSide.Server:
                        subcommand.RequiresPrivilege(Privilege.root);
                        break;
                }
                    
                subcommand.EndSubCommand();
            });
        }
        private TextCommandResult HandleConfigCommand(TextCommandCallingArgs args, PropertyInfo prop, Type propType, (string Name, string Description, string Unit) meta)
        {
            string suffix = string.IsNullOrEmpty(meta.Unit) ? "" : $" {meta.Unit}";

            if (args.Parsers[0].IsMissing)
            {
                string display = FormatConfigValue(prop.GetValue(null), propType);
                return TextCommandResult.Success($"{meta.Name} currently {display}{suffix}");
            }

            object rawValue = args.Parsers[0].GetValue();
            object converted = Convert.ChangeType(rawValue, propType);
            prop.SetValue(null, converted);

            string updatedDisplay = FormatConfigValue(prop.GetValue(null), propType);
            return TextCommandResult.Success($"{meta.Name} now {updatedDisplay}{suffix}");
        }

        private string FormatConfigValue(object value, Type type)
        {
            if (type == typeof(bool)) return (bool)value ? "on" : "off";
            return value?.ToString() ?? "";
        }
    }
}