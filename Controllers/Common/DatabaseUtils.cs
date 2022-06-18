using No1Lib.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TVS_DT_TD.Controllers.Common
{
    public class DatabaseUtils
    {
        public static Database GetDatabase()
        {
            Database db = new Database(Contants.SERVER_DATABASE, Contants.USER_DATABASE, Contants.PASSWORD_DATABASE, Contants.NAME_DATABASE);
            db.Type = DatabaseType.SqlServer;
            return db;
        }
    }
}