# K's Cartograpy Table

The lack of mods for sharing previously created waypoints with other players pushed me to create my own mod, with blackjack and hookers\*. This mod was inspired by [Valeheim's cartography table](https://valheim.fandom.com/wiki/Cartography_table).

## Overview

A cartography table can be built in the crafting grid using a wooden table and a parchment and used by interacting with it holding ink and quill. When a cartography table is placed in the world, players will be able to store their waypoints on it, and copy waypoints added by other users to their map.   
  
**WARNING:** this process is automatic, players won't be able to choose which waypoints to share or to copy. All the waypoints will be copied either from the player's map to the cartography table's map, or the other way around. Players will be able to edit and delete waypoints created by other players, the original owner of the waypoints will see the changes only after interacting with the cartography table to update their player map.

**VERSION 1.0.1 IS EXPERIMENTA**L  
_Backup your savegame before updating, and roll back if there are any issues._

Every cartograpy table can contain a different set of waypoints, so different groups of players can use different cartography tables to manage their group's waypoints.

## Commands

This mod also adds a command to remove all the group ids set on all waypoints by other mods. This should be enough to resolve the index error which can occur after removing mods that add group ids on waypoints, and should be a one time fix.

### Quick guide

*   in the chat window, type /purgewpgroups
*   go to a cartography table
*   right click on it with an empty hand

A chat message should confirm that the group ids have been removed. At the moment this command/interaction only works on local saves, so you can use it to fix your saves open to lan/internet.

The command/interaction will NOT work on a remote server. To fix a server savegame follow this steps:

*   copy the savegame from the server to your pc
*   start the world on your pc in single player
*   run the command/interaction
*   exit the game
*   copy the savegame back from your pc to the server

It's cumbersome but it works, and needs to be done only once.

## Usage

### Updating the cartography table's map

When you right click on the cartography table with ink and quill:

*   the waypoints you added on your map will be copied to the cartography table
*   the waypoints you edited on your map will be updated on the cartography table
*   the waypoints you deleted from your map will be deleted from the cartography table

### Updating your map

When you sprint+right click on the cartography table with ink and quill:

*   the new waypoints on the map will be added to your map
*   the changed waypoints will be updated on your map
*   the delted waypoints will be removed from your map

**DISCLAIMER:** I made this mod for my own server where I play with friends. I might not have time to update it or fix bugs, and it hasn't been extensively tested yet. Be warned, and **backup your saves before using it**!

Special thanks to Trini for the awesome title image!

_\*Blackjack and hookers not included._

### Compatibility

*   compatible with [Egocarib's Auto Map Marker](https://mods.vintagestory.at/show/mod/797)
*   compatible with [NEMI](https://mods.vintagestory.at/nemi) (and possibly other mods which add custom icons)
*   not compatible with other waypoint sharing mods
*   potentially compatible with [Prospect Together](https://mods.vintagestory.at/prospecttogether), additional testing required

### Roadmap

1.  remove waypoints deleted by other users from the player's map on interaction (added in v1.0.1)
2.  add a way to wipe the map on the cartography's table
3.  prevent multiple interactions firing while the player holds down right click
4.  (not sure if possible) also share explored areas of the map
5.  (not sure if possible) view the map stored on the cartography table

### Changelog

1.  v1.0.0 - Initial release
2.  v1.0.1 - Deleted waypoints will be removed from the player's map on update - experimental
