using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UP01_Smolin_Tournaments
{
    public partial class DB_Smolin_TournamentsEntities
    {
        private static DB_Smolin_TournamentsEntities _context;

        public static DB_Smolin_TournamentsEntities GetContext()
        {
            if (_context == null)
                _context = new DB_Smolin_TournamentsEntities();
            return _context;
        }
    }

    public partial class Tournament
    {
        public decimal PlannedEntryRevenue => EntryFee * MaxTeams;
    }
}
