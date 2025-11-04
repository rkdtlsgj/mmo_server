using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public class Item
    {
        public ItemInfo info { get; } = new ItemInfo();

        public int ItemDbid
        {
            get { return info.ItemDbId; }
            set { info.ItemDbId = value; }
        }

        public int Templateid
        {
            get { return info.TemplateId; }
            set { info.TemplateId = value; }
        }

        public int Count
        {
            get { return info.Count; }
            set { info.Count = value; }
        }

        public int Slot
        {
            get { return info.Slot; }
            set { info.Slot = value; }
        }

        public bool Equipped
        {
            get { return info.Equipped; }
            set { info.Equipped = value; }
        }

        public ItemType ItemType { get; private set; }
        public bool Stackable { get; protected set; } //아이템이 누적이 되는 형태인가?

        public Item(ItemType itemType)
        {
            ItemType = itemType;
        }

        public static Item MakeItem(ItemDb itemDb)
        {
            Item item = null;

            ItemData itemData = null;
            DataManager.ItemDict.TryGetValue(itemDb.TemplateId, out itemData);

            if (itemData == null)
                return null;

            switch (itemData.itemType)
            {
                case ItemType.Weapon:
                    item = new Weapon(itemDb.TemplateId);
                    break;

                case ItemType.Armor:
                    item = new Armor(itemDb.TemplateId);
                    break;

                case ItemType.Consumable:
                    item = new Consumable(itemDb.TemplateId);
                    break;
            }

            if (item != null)
            {
                item.ItemDbid = itemDb.ItemDbId;
                item.Count = itemDb.Count;
                item.Slot = itemDb.Slot;
                item.Equipped = itemDb.Equipped;
            }


            return item;
        }
    }

    public class Weapon : Item
    {
        public WeaponType WeaponType { get; private set; }
        public int Damage { get; private set; }
        public Weapon(int templateId) : base(ItemType.Weapon)
        {
            Init(templateId);
        }

        void Init(int templateId)
        {
            ItemData itemData = null;
            DataManager.ItemDict.TryGetValue(templateId, out itemData);

            if (itemData.itemType != ItemType.Weapon)
                return;

            WeaponData data = (WeaponData)itemData;
            {
                Templateid = data.id;
                Count = 1;
                WeaponType = data.weaponType;
                Damage = data.damage;
                Stackable = false;
            }

        }
    }

    public class Armor : Item
    {
        public ArmorType ArmorType { get; private set; }
        public int Defence { get; private set; }
        public Armor(int templateId) : base(ItemType.Armor)
        {
            Init(templateId);
        }

        void Init(int templateId)
        {
            ItemData itemData = null;
            DataManager.ItemDict.TryGetValue(templateId, out itemData);

            if (itemData.itemType != ItemType.Armor)
                return;

            ArmorData data = (ArmorData)itemData;
            {
                Templateid = data.id;
                Count = 1;
                ArmorType = data.armorType;
                Defence = data.defence;
                Stackable = false;
            }

        }
    }

    public class Consumable : Item
    {
        public ConsumableType ConsumableType { get; private set; }
        public int MaxCount { get; private set; }
        public Consumable(int templateId) : base(ItemType.Consumable)
        {
            Init(templateId);
        }

        void Init(int templateId)
        {
            ItemData itemData = null;
            DataManager.ItemDict.TryGetValue(templateId, out itemData);

            if (itemData.itemType != ItemType.Consumable)
                return;

            ConsumableData data = (ConsumableData)itemData;
            {
                Templateid = data.id;
                Count = 1;
                MaxCount = data.maxCount;
                ConsumableType = data.consumableType;
                Stackable = (data.maxCount > 1);
            }
        }
    }
}