using Google.Protobuf.Protocol;
using Server.DB;
using Server.Game.Room;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Server.Game
{
    public class Player : GameObject
    {
        public int PlayerDbid{get;set;}
        public ClientSession Session { get; set; }
        public VisionCube Vision { get; private set; }
        public Inventory Inven { get; set; } = new Inventory();

        //추가적인 스탯 관련 
        public int WeaponDamage { get; private set; }
        public int ArmorDefence { get; private set; }


        public override int TotalAttack { get { return Stat.Attack + WeaponDamage; } }
        public override int TotalDefence { get { return ArmorDefence; } }

        public Player()
        {
            ObjectType = GameObjectType.Player;
            Vision = new VisionCube(this);
        }

        public override void OnDamaged(GameObject attacker, int damage)
        {            
            base.OnDamaged(attacker, damage);

            //데미지를 입을때마다 디비연동???
            //디비에 계속접근하는건 안좋음            
        }

        public override void OnDead(GameObject attacker)
        {
            base.OnDead(attacker);
        }


        public void OnLeaveGame()
        {
            DbTransaction.SavePlayerStatus_AllInOne(this, Room);            
        }


        public void HandleEquipItem(C_EquipItem equipPacket)
        {
            //인벤에 아이템이 존재하는가 확인
            Item item = Inven.Get(equipPacket.ItemDbId);
            if (item == null)
                return;

            if (item.ItemType == ItemType.Consumable)
                return;

            if (equipPacket.Equipped == true)
            {
                Item unequipitem = null;

                if (item.ItemType == ItemType.Weapon)
                {
                    unequipitem = Inven.Find(i => i.Equipped == true && i.ItemType == ItemType.Weapon);
                }
                else if (item.ItemType == ItemType.Armor)
                {
                    ArmorType armorType = ((Armor)item).ArmorType;

                    unequipitem = Inven.Find(i => i.Equipped == true &&
                    i.ItemType == ItemType.Armor &&
                    ((Armor)i).ArmorType == armorType);
                }

                if (unequipitem != null)
                {
                    unequipitem.Equipped = false;

                    DB.DbTransaction.EquipItemNoti(this, unequipitem);

                    S_EquipItem equipItem = new S_EquipItem();
                    equipItem.ItemDbId = unequipitem.ItemDbid;
                    equipItem.Equipped = unequipitem.Equipped;

                    Session.Send(equipItem);
                }
            }

            {
                //메모리 선 적용 후 DB에 알려주는 방식
                item.Equipped = equipPacket.Equipped;

                DB.DbTransaction.EquipItemNoti(this, item);

                S_EquipItem equipItem = new S_EquipItem();
                equipItem.ItemDbId = equipPacket.ItemDbId;
                equipItem.Equipped = equipPacket.Equipped;

                Session.Send(equipItem);
            }

            RefreshAdditionalStat();
        }

        public void RefreshAdditionalStat()
        {
            WeaponDamage = 0;
            ArmorDefence = 0;

            foreach (Item item in Inven.Items.Values)
            {
                if (item.Equipped == false)                
                    continue;

                switch (item.ItemType)
                {
                    case ItemType.Weapon:
                        WeaponDamage += ((Weapon)item).Damage;
                        break;

                    case ItemType.Armor:
                        ArmorDefence += ((Armor)item).Defence;
                        break;
                }
            }
        }
    }
}
