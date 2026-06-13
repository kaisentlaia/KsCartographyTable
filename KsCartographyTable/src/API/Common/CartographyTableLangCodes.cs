namespace Kaisentlaia.KsCartographyTableMod.API.Common
{
	public static class CartographyTableLangCodes
	{
		public readonly static string PLAYER_WAYPOINTS_ADDED = GetLangCode("message-new-user-waypoints");
		public readonly static string PLAYER_WAYPOINTS_DELETED = GetLangCode("message-deleted-user-waypoints");
		public readonly static string PLAYER_WAYPOINTS_EDITED = GetLangCode("message-edited-user-waypoints");
		public readonly static string PLAYER_MAP_UPDATED = GetLangCode("message-updated-user-explored-chunks");
		public readonly static string PLAYER_WAYPOINTS_UP_TO_DATE = GetLangCode("message-user-waypoints-up-to-date");
		public readonly static string PLAYER_WAYPOINTS_REJECTED = GetLangCode("message-user-waypoints-rejected");
		public readonly static string PLAYER_MAP_UP_TO_DATE = GetLangCode("message-user-map-up-to-date");
		public readonly static string TABLE_MAP_UP_TO_DATE = GetLangCode("message-table-map-up-to-date");
		public readonly static string TABLE_MAP_UPDATED = GetLangCode("message-updated-map-explored-chunks");
		public readonly static string TABLE_MAP_WIPED = GetLangCode("message-table-map-wiped");
		public readonly static string TABLE_MAP_ALREADY_EMPTY = GetLangCode("message-table-map-already-empty");
		public readonly static string TABLE_WAYPOINTS_UP_TO_DATE = GetLangCode("message-table-waypoints-up-to-date");
		public readonly static string TABLE_WAYPOINTS_ADDED = GetLangCode("message-new-waypoints-count");
		public readonly static string TABLE_WAYPOINTS_DELETED = GetLangCode("message-deleted-waypoints-count");
		public readonly static string TABLE_WAYPOINTS_EDITED = GetLangCode("message-edited-waypoints-count");
		public readonly static string INTERACTION_TABLE_UPDATE = GetLangCode("blockhelp-cartography-table-share-map");
		public readonly static string INTERACTION_TABLE_WIPE = GetLangCode("blockhelp-cartography-table-wipe-map");
		public readonly static string INTERACTION_TABLE_PONDER = GetLangCode("blockhelp-cartography-table-ponder");
		public readonly static string INTERACTION_USER_UPDATE = GetLangCode("blockhelp-cartography-table-update-map");
		public readonly static string INTERACTION_ADD_QUILL = GetLangCode("blockhelp-cartography-table-addquill");
		public readonly static string INTERACTION_REMOVE_QUILL = GetLangCode("blockhelp-cartography-table-removequill");
		public readonly static string GUI_TABLE_WAYPOINTS = GetLangCode("gui-waypoint-count");
		public readonly static string GUI_TABLE_MAP_WAYPOINTS = GetLangCode("gui-waypoint-chunks-count");
		public readonly static string GUI_TABLE_EMPTY = GetLangCode("gui-empty-map");

        public readonly static string SESSION_STARTED = GetLangCode("message-session-started");
        public readonly static string WIPE_STARTED = GetLangCode("message-wipe-started");
        public readonly static string FAILURE_UPDATE_FIRST = GetLangCode("mapfailure-updatefirst");
        public readonly static string FAILURE_BUSY = GetLangCode("mapfailure-busy");
        public readonly static string CONFIG_IMMERSIVE_MODE_NAME = GetLangCode("config-setting-immersivemode-name");
        public readonly static string CONFIG_IMMERSIVE_MODE_COMMENT = GetLangCode("config-setting-immersivemode-comment");
        public readonly static string CONFIG_CHUNKS_PER_PACKET_NAME = GetLangCode("config-setting-chunksperpacket-name");
        public readonly static string CONFIG_CHUNKS_PER_PACKET_COMMENT = GetLangCode("config-setting-chunksperpacket-comment");
        public readonly static string CONFIG_PACKET_DELAY_NAME = GetLangCode("config-setting-packetdelay-name");
        public readonly static string CONFIG_PACKET_DELAY_COMMENT = GetLangCode("config-setting-packetdelay-comment");
        public readonly static string CONFIG_VERBOSE_DEBUG_NAME = GetLangCode("config-setting-verbosedebug-name");
        public readonly static string CONFIG_VERBOSE_DEBUG_COMMENT = GetLangCode("config-setting-verbosedebug-comment");
        public readonly static string CONFIG_WAYPOINT_DOWNLOAD_NAME = GetLangCode("config-setting-waypointdownload-name");
        public readonly static string CONFIG_WAYPOINT_DOWNLOAD_COMMENT = GetLangCode("config-setting-waypointdownload-comment");
        public readonly static string CONFIG_WAYPOINT_UPLOAD_NAME = GetLangCode("config-setting-waypointupload-name");
        public readonly static string CONFIG_WAYPOINT_UPLOAD_COMMENT = GetLangCode("config-setting-waypointupload-comment");

		private static string GetLangCode(string code)
		{
			return CartographyTableConstants.MOD_ID + ":" + code;
		}
	}
}