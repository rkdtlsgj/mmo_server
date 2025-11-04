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
        public static DbTransaction Instance { get; } = new DbTransaction();

        public static void SavePlayerStatus_AllInOne(Player player, GameRoom room)
        {
            if(player == null || room == null) 
                return;

            PlayerDb playerDb = new PlayerDb();
            playerDb.PlayerDbId = player.PlayerDbid;
            playerDb.Hp = player.Stat.Hp;


            //DB 접근할때는 Job을 던져줘서 실행한다.
            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Entry(playerDb).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                    db.Entry(playerDb).Property(nameof(PlayerDb.Hp)).IsModified = true;
                    bool success = db.SaveChangesEx();
                    if (success == false)
                        return;
                }
            });           
        }

        public static void RewardPlayer(Player player, RewardData rewardData, GameRoom room)
        {
            if (player == null || room == null || rewardData == null)
                return;


            //동시처리의 문제가 존재
            int? slot = player.Inven.GetEmptySlot();
            if (slot == null)
                return;

            ItemDb itemDb = new ItemDb()
            {
                TemplateId = rewardData.itemId,
                Count = rewardData.count,
                Slot = slot.Value,
                OwnerDbid = player.PlayerDbid
            };


            Instance.Push(() =>
            {
                using (AppDbContext db = new AppDbContext())
                {
                    db.Items.Add(itemDb);
                    bool success = db.SaveChangesEx();
                    if (success == false)
                        return;

                    //완료가됐으면 다시 알림
                    room.Push(() =>
                    {
                        Item newItem = Item.MakeItem(itemDb);
                        player.Inven.Add(newItem);

                        //클라이언트에게 전송!
                        {
                            S_AddItem itemPacket = new S_AddItem();
                            ItemInfo itemInfo = new ItemInfo();
                            itemInfo.MergeFrom(newItem.info);

                            itemPacket.Items.Add(itemInfo);

                            player.Session.Send(itemPacket);
                        }

                    });
                }
            });
        }
    }
}
