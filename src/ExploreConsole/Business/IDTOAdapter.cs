using ExploreConsole.DTO;
using ExploreConsole.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExploreConsole.Business
{

    public interface IDTOAdapter
    {

        DTO.MapSession CreateMapSessionObject(Entities.Map map, List<Entities.Item> items);

    }

}
