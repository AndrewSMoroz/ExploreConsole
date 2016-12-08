using ExploreConsole.DTO;
using ExploreConsole.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExploreConsole.Business
{

    public interface IBusinessManager
    {

        MapSession GetInitialMapSession(int mapID);
        MapState ProcessAction(MapSession mapSession);

    }

}
