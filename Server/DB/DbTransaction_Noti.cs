using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DB
{

    //DB 작업을 대신 처리해준다
    public partial class DbTransaction : JobSerializer
    {
        public static void EquipItemNoti(Player player, Item item)
        {
            if (player == null || item == null)
                return;


            ItemDb itemDb = new ItemDb()
            {
                ItemDbId = item.ItemDbid,
                Equipped = item.Equipped
            };


            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(itemDb).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                    db.Entry(itemDb).Property(nameof(itemDb.Equipped)).IsModified = true;

                    bool success = db.SaveChangesEx();
                    if (success == false)
                    {
                        
                    }
                }
            });
        }
    }
}
