namespace Kaisentlaia.KsCartographyTableMod.API.Common
{
	public static class CartographyTableLangCodes
	{
		public readonly static string USER_WAYPOINTS_ADDED = GetLangCode("message-new-user-waypoints");
		public readonly static string USER_WAYPOINTS_DELETED = GetLangCode("message-deleted-user-waypoints");
		public readonly static string USER_WAYPOINTS_EDITED = GetLangCode("message-edited-user-waypoints");
		public readonly static string USER_MAP_UPDATED = GetLangCode("message-updated-user-explored-chunks");
		public readonly static string USER_WAYPOINTS_UP_TO_DATE = GetLangCode("message-user-waypoints-up-to-date");
		public readonly static string USER_MAP_UP_TO_DATE = GetLangCode("message-user-map-up-to-date");
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
		public readonly static string GUI_TABLE_WAYPOINTS = GetLangCode("gui-waypoint-count");
		public readonly static string GUI_TABLE_MAP_WAYPOINTS = GetLangCode("gui-waypoint-chunks-count");
		public readonly static string GUI_TABLE_EMPTY = GetLangCode("gui-empty-map");

		private static string GetLangCode(string code)
		{
			return CartographyTableConstants.MOD_ID + ":" + code;
		}
	}
}