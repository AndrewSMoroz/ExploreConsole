using ExploreConsole.Business;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ExploreConsole
{

    public class Program
    {

        public static void Main(string[] args)
        {

            Console.WriteLine();
            Console.WriteLine("Starting Explore application");
            Console.WriteLine();
            Console.Write("Reading from configuration sources...");

            // TODO: How to do this in a type-safe way?  Create a class that holds the expected settings?
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddInMemoryCollection(new Dictionary<string, string>        // Hard coded - can serve as a default
            {
                { "MapID", "1" },
                { "Cursor", ">" }
            });
            builder.AddCommandLine(args);                                       // Comes from command line agrguments like --MapID 5
            
            IConfigurationRoot config = builder.Build();

            Console.WriteLine("  Finished.");
            Console.WriteLine();

            // NOTE: Since there is no formal dependency injection container in use, the static Main method functions as the container
            //       for this application.  All injected objects have a scope of the application lifetime.

            IDTOAdapter dtoAdapter = new DTOAdapter();
            IBusinessManager businessManager = new BusinessManager(dtoAdapter);
            MapSessionEngine mapSessionEngine = new MapSessionEngine(config, businessManager);

            try
            {
                mapSessionEngine.BeginSession();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.Write("ERROR: ");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

        }

    }

    #region Example Code

    //------------------------------------------------------------------------------------------------------------------
    // Example of reading config settings from multiple sources
    // When it's finished, you just access it as a Dictionary
    // If the same key appears in more than one source, the last one wins
    //------------------------------------------------------------------------------------------------------------------

    //ConfigurationBuilder builder = new ConfigurationBuilder();
    //builder.SetBasePath(Directory.GetCurrentDirectory());
    //        builder.AddJsonFile("appsettings.json");                            // Read from settings file
    //        builder.AddInMemoryCollection(new Dictionary<string, string>        // Hard coded - can serve as a default
    //        {
    //            { "MapID", "1" }
    //        });
    //        builder.AddCommandLine(args);                                       // Comes from command line agrguments like --MapID 5

    //------------------------------------------------------------------------------------------------------------------
    // Enumerate and display the settings in the IConfigurationRoot
    //------------------------------------------------------------------------------------------------------------------

    //Console.WriteLine("Configuration Settings:");
    //foreach (KeyValuePair<string, string> kvp in config.AsEnumerable())
    //{
    //    Console.WriteLine("{0} : {1}", kvp.Key, kvp.Value);
    //}

    #endregion Example Code

}
