﻿using ExploreConsole.Data;
using ExploreConsole.DTO;
using ExploreConsole.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExploreConsole.Business
{

    public class BusinessManager : IBusinessManager
    {

        private readonly IDTOAdapter _dtoAdapter;

        //--------------------------------------------------------------------------------------------------------------
        public BusinessManager(IDTOAdapter dtoApapter)
        {
            _dtoAdapter = dtoApapter;
        }
  
        //--------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Returns an object containing the definition of the specified map ID, and initializes the state of the user's session in that map
        /// </summary>
        public MapSession GetInitialMapSession(int mapID)
        {

            MapSession mapSession = new MapSession();
            Entities.Map map;
            List<Entities.Item> items;

            // Retrieve domain model entites from data layer
            using (ExploreContext context = ExploreContextFactory.Create())
            {

                map = context.Maps
                             .Include(m => m.Locations)
                                  .ThenInclude(l => l.LocationConnectionFromLocations)
                             .AsNoTracking()
                             .SingleOrDefault(m => m.Id == mapID);

                items = context.Items
                               .Where(i => i.Location.MapId == mapID)
                               .AsNoTracking()
                               .ToList();

            }

            // Transform domain model entites into DTOs and return
            mapSession = _dtoAdapter.CreateMapSessionObject(map, items);
            ProcessActionLook(mapSession);
            return mapSession;

        }

        //--------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Processes the action contained in the specified MapSession's MapState.RequestedAction property.
        /// </summary>
        /// <returns>A MapState object representing the state of the map after the action was processed.</returns>
        public MapState ProcessAction(MapSession mapSession)
        {

            if (mapSession == null) { throw new Exception("MapSession object is null"); }
            if (mapSession.MapDefinition == null) { throw new Exception("MapDefinition object is null"); }
            if (mapSession.MapState == null) { throw new Exception("MapState object is null"); }

            mapSession.MapState.ActionResultMessages = new List<string>();

            // TODO: To remove hard coding, you could create tables Action and ActionType
            //       You could call the appropriate ProcessXYZAction() method based on the associated ActionType
            //       The Action table could have a long and a short form for each action, e.g. N and NORTH
            //       You might want to read the actions on startup and cache them, or even have the UI cache and
            //       evaluate them so you wouldn't need a round trip to validate input actions

            switch ((mapSession.MapState.RequestedAction ?? "").ToUpper())
            {
                case "NORTH":
                case "N":
                case "SOUTH":
                case "S":
                case "EAST":
                case "E":
                case "WEST":
                case "W":
                case "NORTHEAST":
                case "NE":
                case "NORTHWEST":
                case "NW":
                case "SOUTHEAST":
                case "SE":
                case "SOUTHWEST":
                case "SW":
                case "UP":
                case "U":
                case "DOWN":
                case "D":
                    ProcessActionMovement(mapSession);
                    break;
                case "LOOK":
                    ProcessActionLook(mapSession);
                    break;
                case "INSPECT":
                    ProcessActionInspect(mapSession);
                    break;
                case "INVENTORY":
                case "I":
                    ProcessActionInventory(mapSession);
                    break;
                case "TAKE":
                    ProcessActionTake(mapSession);
                    break;
                case "DROP":
                    ProcessActionDrop(mapSession);
                    break;
                case "SAVE":
                    ProcessActionSave(mapSession);
                    break;
                case "RESTORE":
                    mapSession = ProcessActionRestore(mapSession);
                    break;
                case "HELP":
                    ProcessActionHelp(mapSession);
                    break;
                default:
                    mapSession.MapState.ActionResultMessages.Add("Unknown command.");
                    break;
            }

            return mapSession.MapState;

        }

        //--------------------------------------------------------------------------------------------------------------
        private void ProcessActionMovement(MapSession mapSession)
        {

            string direction = null;

            // TODO: This will become much more compact, with no hardcodes, if the actions are stored in a table
            switch (mapSession.MapState.RequestedAction)
            {
                case "NORTH":
                    direction = "N";
                    break;
                case "SOUTH":
                    direction = "S";
                    break;
                case "EAST":
                    direction = "E";
                    break;
                case "WEST":
                    direction = "W";
                    break;
                case "NORTHEAST":
                    direction = "NE";
                    break;
                case "NORTHWEST":
                    direction = "NW";
                    break;
                case "SOUTHEAST":
                    direction = "SE";
                    break;
                case "SOUTHWEST":
                    direction = "SW";
                    break;
                case "UP":
                    direction = "U";
                    break;
                case "DOWN":
                    direction = "D";
                    break;
                default:
                    direction = mapSession.MapState.RequestedAction;
                    break;
            }

            DTO.LocationConnection connection = mapSession.MapDefinition
                                                           .Map
                                                           .Locations
                                                           .Single(l => l.Id == mapSession.MapState.CurrentLocationID)
                                                           .LocationConnections
                                                           .SingleOrDefault(lc => lc.Direction == direction);

            if (connection == null)
            {
                mapSession.MapState.ActionResultMessages.Add("You can't go that direction.");
            }
            else
            {
                mapSession.MapState.CurrentLocationID = connection.ToLocationId;
                ProcessActionLook(mapSession);
            }

        }

        //--------------------------------------------------------------------------------------------------------------
        private void ProcessActionLook(MapSession mapSession)
        {

            DTO.Location currentLocation = mapSession.MapDefinition.Map.Locations.Single(l => l.Id == mapSession.MapState.CurrentLocationID);
            mapSession.MapState.ActionResultMessages.Add(currentLocation.Name);

            List<DTO.Item> items = mapSession.MapState.Items.Where(i => i.LocationId == mapSession.MapState.CurrentLocationID).ToList();
            if (items.Any())
            {
                mapSession.MapState.ActionResultMessages.Add("");
                foreach (DTO.Item item in items)
                {
                    mapSession.MapState.ActionResultMessages.Add(item.BuildGeneralDescription());
                }
            }

        }

        //--------------------------------------------------------------------------------------------------------------
        public void ProcessActionInspect(MapSession mapSession)
        {

            if (string.IsNullOrEmpty(mapSession.MapState.RequestedActionTarget))
            {
                mapSession.MapState.ActionResultMessages.Add("What would you like to inspect?");
                return;
            }

            List<DTO.Item> items = mapSession.MapState.Items.Where(i => i.LocationId == mapSession.MapState.CurrentLocationID || i.LocationId == 0).ToList();
            DTO.Item item = items.SingleOrDefault(i => (i.Name ?? "").ToUpper() == mapSession.MapState.RequestedActionTarget);

            if (item == null)
            {
                mapSession.MapState.ActionResultMessages.Add("I can't seem to find what you want to inspect.");
            }
            else
            {
                mapSession.MapState.ActionResultMessages.Add(item.Description + (item.LocationId == 0 ? " (in inventory)" : ""));
            }

        }

        //--------------------------------------------------------------------------------------------------------------
        private void ProcessActionTake(MapSession mapSession)
        {

            if (string.IsNullOrEmpty(mapSession.MapState.RequestedActionTarget))
            {
                mapSession.MapState.ActionResultMessages.Add("What would you like to take?");
                return;
            }

            List<DTO.Item> items = mapSession.MapState.Items.Where(i => i.LocationId == mapSession.MapState.CurrentLocationID || i.LocationId == 0).ToList();
            DTO.Item item = items.SingleOrDefault(i => (i.Name ?? "").ToUpper() == mapSession.MapState.RequestedActionTarget);

            if (item == null)
            {
                mapSession.MapState.ActionResultMessages.Add("I can't seem to find what you want to take.");
            }
            else if (item.LocationId == 0)
            {
                mapSession.MapState.ActionResultMessages.Add("You are already carrying " + (item.Plural ? "those" : "that") + ".");
            }
            else
            {
                item.LocationId = 0;
                mapSession.MapState.ActionResultMessages.Add("Taken.");
            }

        }

        //--------------------------------------------------------------------------------------------------------------
        private void ProcessActionDrop(MapSession mapSession)
        {

            if (string.IsNullOrEmpty(mapSession.MapState.RequestedActionTarget))
            {
                mapSession.MapState.ActionResultMessages.Add("What would you like to drop?");
                return;
            }

            DTO.Item item = mapSession.MapState.Items.SingleOrDefault(i => i.LocationId == 0 && (i.Name ?? "").ToUpper() == mapSession.MapState.RequestedActionTarget);

            if (item == null)
            {
                mapSession.MapState.ActionResultMessages.Add("You're not carrying that.");
            }
            else
            {
                item.LocationId = mapSession.MapState.CurrentLocationID;
                mapSession.MapState.ActionResultMessages.Add("Dropped.");
            }

        }

        //--------------------------------------------------------------------------------------------------------------
        private void ProcessActionInventory(MapSession mapSession)
        {

            List<DTO.Item> items = mapSession.MapState.Items.Where(i => i.LocationId == 0).ToList();
            if (items == null || !items.Any())
            {
                mapSession.MapState.ActionResultMessages.Add("You are not carrying anything.");
            }
            else
            {
                mapSession.MapState.ActionResultMessages.Add("You are carrying:");
                mapSession.MapState.ActionResultMessages.Add("");
                foreach (DTO.Item item in items)
                {
                    mapSession.MapState.ActionResultMessages.Add(item.BuildInventoryDescription());
                }
            }

        }

        //--------------------------------------------------------------------------------------------------------------
        private void ProcessActionSave(MapSession mapSession)
        {

            using (ExploreContext context = ExploreContextFactory.Create())
            {

                MapSessionSave mapSessionSave = context.MapSessionSaves
                                                       .OrderByDescending(s => s.SaveDateTime)
                                                       .FirstOrDefault(s => s.MapId == mapSession.MapState.MapID);

                if (mapSessionSave == null)
                {
                    mapSessionSave = new MapSessionSave();
                    mapSessionSave.MapId = mapSession.MapState.MapID;
                    context.Add(mapSessionSave);
                }

                string saveData = JsonConvert.SerializeObject(mapSession);
                mapSessionSave.SaveData = saveData;
                mapSessionSave.SaveDateTime = DateTime.Now;

                context.SaveChanges();

                mapSession.MapState.ActionResultMessages.Add("Saved.");

            }

        }

        //--------------------------------------------------------------------------------------------------------------
        private MapSession ProcessActionRestore(MapSession mapSession)
        {

            MapSessionSave mapSessionSave = null;

            using (ExploreContext context = ExploreContextFactory.Create())
            {

                mapSessionSave = context.MapSessionSaves
                                        .OrderByDescending(s => s.SaveDateTime)
                                        .AsNoTracking()
                                        .FirstOrDefault(s => s.MapId == mapSession.MapState.MapID);

            }

            if (mapSessionSave == null)
            {
                mapSession.MapState.ActionResultMessages.Add("Could not find a saved game to restore.");
                return mapSession;
            }
            else
            {

                MapSession restoredMapSession = JsonConvert.DeserializeObject<MapSession>(mapSessionSave.SaveData);

                restoredMapSession.MapState.ActionResultMessages = new List<string>();
                restoredMapSession.MapState.ActionResultMessages.Add("Restored.");
                restoredMapSession.MapState.ActionResultMessages.Add("");
                ProcessActionLook(restoredMapSession);

                return restoredMapSession;

            }

        }

        //--------------------------------------------------------------------------------------------------------------
        private void ProcessActionHelp(MapSession mapSession)
        {
            // TODO: This will change to a loop if the actions are stored in a table
            mapSession.MapState.ActionResultMessages.Add("Recognized commands:");
            mapSession.MapState.ActionResultMessages.Add("");
            mapSession.MapState.ActionResultMessages.Add("NORTH (N)");
            mapSession.MapState.ActionResultMessages.Add("SOUTH (S)");
            mapSession.MapState.ActionResultMessages.Add("EAST (E)");
            mapSession.MapState.ActionResultMessages.Add("WEST (W)");
            mapSession.MapState.ActionResultMessages.Add("NORTHEAST (NE)");
            mapSession.MapState.ActionResultMessages.Add("NORTHWEST (NW)");
            mapSession.MapState.ActionResultMessages.Add("SOUTHEAST (SE)");
            mapSession.MapState.ActionResultMessages.Add("SOUTHWEST (SW)");
            mapSession.MapState.ActionResultMessages.Add("UP (U)");
            mapSession.MapState.ActionResultMessages.Add("DOWN (D)");
            mapSession.MapState.ActionResultMessages.Add("LOOK");
            mapSession.MapState.ActionResultMessages.Add("INSPECT");
            mapSession.MapState.ActionResultMessages.Add("INVENTORY (I)");
            mapSession.MapState.ActionResultMessages.Add("TAKE");
            mapSession.MapState.ActionResultMessages.Add("DROP");
            mapSession.MapState.ActionResultMessages.Add("SAVE");
            mapSession.MapState.ActionResultMessages.Add("RESTORE");
            mapSession.MapState.ActionResultMessages.Add("HELP");
            mapSession.MapState.ActionResultMessages.Add("EXIT");

        }

    }

}
