using ExploreConsole.DTO;
using ExploreConsole.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExploreConsole.Business
{

    public static class ItemExtensions
    {

        //--------------------------------------------------------------------------------------------------------------
        public static string BuildGeneralDescription(this DTO.Item item)
        {
            return "There " + (item.Plural ? "are" : "is") + " " + item.Determiner + " " + item.Name + " here.";
        }

        //--------------------------------------------------------------------------------------------------------------
        public static string BuildInventoryDescription(this DTO.Item item)
        {
            return item.Determiner + " " + item.Name;
        }

    }

}
