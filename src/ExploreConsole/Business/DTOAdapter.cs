using ExploreConsole.DTO;
using ExploreConsole.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExploreConsole.Business
{

    /// <summary>
    /// Use this class to transform domain model entities into DTOs to send to clients
    /// </summary>
    public class DTOAdapter : IDTOAdapter
    {

        //--------------------------------------------------------------------------------------------------------------
        public DTO.MapSession CreateMapSessionObject(Entities.Map map, List<Entities.Item> items)
        {

            // Validate that a map and at least one location is defined

            if (map == null || map.Locations == null || !map.Locations.Any())
            {
                throw new Exception("Could not find valid map and location data for MapID " + map.Id.ToString());
            }

            // Validate that exactly one initial location is defined

            List<Entities.Location> initialLocations = map.Locations
                                                 .Where(l => l.IsInitialLocation == true)
                                                 .ToList();

            if (initialLocations == null || !initialLocations.Any())
            {
                throw new Exception("No initial location defined for MapID " + map.Id.ToString());
            }

            if (initialLocations.Count > 1)
            {
                throw new Exception("More than one initial location defined for MapID " + map.Id.ToString());
            }

            // Transform the Entities objects into DTO objects

            DTO.MapSession newMapSession = new DTO.MapSession();

            newMapSession.MapDefinition = new DTO.MapDefinition();
            newMapSession.MapDefinition.Map = new DTO.Map() { Id = map.Id, Name = map.Name };
            List<DTO.Location> newLocations = new List<DTO.Location>();
            foreach (Entities.Location l in map.Locations)
            {
                DTO.Location newLocation = new DTO.Location() { Id = l.Id, Description = l.Description, IsInitialLocation = l.IsInitialLocation, MapId = l.MapId, Name = l.Name };
                List<DTO.LocationConnection> newLocationConnections = new List<DTO.LocationConnection>();
                foreach (Entities.LocationConnection lc in l.LocationConnectionFromLocations)
                {
                    DTO.LocationConnection newLocationConnection = new DTO.LocationConnection () { Id = lc.Id, ToLocationId = lc.ToLocationId, Direction = lc.Direction };
                    newLocationConnections.Add(newLocationConnection);
                }
                newLocation.LocationConnections = newLocationConnections;
                newLocations.Add(newLocation);
            }
            newMapSession.MapDefinition.Map.Locations = newLocations;

            newMapSession.MapState = new DTO.MapState();
            newMapSession.MapState.MapID = map.Id;
            newMapSession.MapState.CurrentLocationID = initialLocations[0].Id;
            newMapSession.MapState.ActionResultMessages = new List<string>();
            List<DTO.Item> newItems = new List<DTO.Item>();
            foreach (Entities.Item i in items)
            {
                newItems.Add(new DTO.Item() { Id = i.Id, Description = i.Description, Determiner = i.Determiner, LocationId = i.LocationId, Name = i.Name, Plural = i.Plural });
            }
            newMapSession.MapState.Items = newItems;

            return newMapSession;

        }

    }

}
