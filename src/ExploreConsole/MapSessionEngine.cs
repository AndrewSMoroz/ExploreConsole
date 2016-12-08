using ExploreConsole.Business;
using ExploreConsole.DTO;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExploreConsole
{

    /// <summary>
    /// This class is responsible for obtaining user input, calling business methods with that input, and 
    /// providing the resulting output to the user.
    /// </summary>
    public class MapSessionEngine
    {

        private readonly IConfigurationRoot _config;
        private readonly IBusinessManager _businessManager;
        private MapSession _mapSession;

        //--------------------------------------------------------------------------------------------------------------
        public MapSessionEngine(IConfigurationRoot configurationRoot, IBusinessManager businessManager)
        {
            _config = configurationRoot;
            _businessManager = businessManager;
        }

        //--------------------------------------------------------------------------------------------------------------
        public void BeginSession()
        {
            
            // Validate MapID in _config

            int mapID = 0;
            int.TryParse(_config["MapID"], out mapID);

            if (mapID < 1)
            {
                Console.WriteLine("Invalid value for MapID, exiting application.");
                return;
            }

            // Fetch _mapSession from business manager

            Console.Write("Fetching map data...");
            _mapSession = _businessManager.GetInitialMapSession(mapID);
            if (_mapSession == null) { throw new Exception("MapSession object is null"); }
            if (_mapSession.MapDefinition == null) { throw new Exception("MapDefinition object is null"); }
            if (_mapSession.MapState == null) { throw new Exception("MapState object is null"); }
            Console.WriteLine("  Finished.");
            Console.WriteLine();

            // Display startup status info

            Console.WriteLine("Beginning session for Map {0} - {1}", mapID.ToString(), _mapSession.MapDefinition.Map.Name);
            Console.WriteLine();
            Console.WriteLine("Type 'HELP' for a list of recognized commands.");
            Console.WriteLine();
            OutputActionResultMessages();

            // Begin game loop

            bool isGameInProgress = true;
            string userInput = "";
            string[] userInputTokens = null;

            while (isGameInProgress)
            {

                // Get user input
                Console.Write(_config["Cursor"]);
                userInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userInput)) { continue; }

                // Put the first word into the RequestedAction property; and the rest, if any, into RequestedActionTarget
                _mapSession.MapState.RequestedAction = null;
                _mapSession.MapState.RequestedActionTarget = null;
                userInputTokens = userInput.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                _mapSession.MapState.RequestedAction = userInputTokens[0].ToUpper();
                if (userInputTokens.Length > 1)
                {
                    _mapSession.MapState.RequestedActionTarget = string.Join(" ", userInputTokens, 1, userInputTokens.Length - 1).ToUpper();
                }

                if (_mapSession.MapState.RequestedAction == "EXIT")
                {
                    isGameInProgress = false;
                    continue;
                }

                // Pass the requested action to the business layer for processing
                _mapSession.MapState = _businessManager.ProcessAction(_mapSession);

                // Output the results of the requested action
                OutputActionResultMessages();

            }

        }

        //--------------------------------------------------------------------------------------------------------------
        private void OutputActionResultMessages()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine();
            foreach (string message in _mapSession.MapState.ActionResultMessages)
            {
                Console.WriteLine(message);
            }
            Console.WriteLine();
            Console.ResetColor();
        }

    }

}
