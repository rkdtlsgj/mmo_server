using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Room
{
    //시야각 클래스
    public  class VisionCube
    {
        public Player Owner { get; private set; }
        public HashSet<GameObject> PreviousObjects {get; private set; } = new HashSet<GameObject>();

        public VisionCube(Player owner)
        {
            Owner = owner;
        }

        //주변에 물체 긁어오기
        public HashSet<GameObject> GetherObjects()
        {
            if (Owner == null || Owner.Room == null)
                return null;

            HashSet<GameObject> objects = new HashSet<GameObject>();

            Vector2Int cellPos = Owner.CellPos;
            List<Zone> zones = Owner.Room.GetAdjsacentZones(cellPos);

            foreach (Zone zone in zones)
            {
                foreach (Player player in zone.Players)
                {
                    int dx = player.CellPos.x - cellPos.x;
                    int dy = player.CellPos.y - cellPos.y;

                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;

                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;

                    objects.Add(player);
                }

                foreach (Monster monster in zone.Monsters)
                {
                    int dx = monster.CellPos.x - cellPos.x;
                    int dy = monster.CellPos.y - cellPos.y;

                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;

                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;

                    objects.Add(monster);
                }

                foreach (Projectile projectile in zone.Projectiles)
                {
                    int dx = projectile.CellPos.x - cellPos.x;
                    int dy = projectile.CellPos.y - cellPos.y;

                    if (Math.Abs(dx) > GameRoom.VisionCells)
                        continue;

                    if (Math.Abs(dy) > GameRoom.VisionCells)
                        continue;

                    objects.Add(projectile);
                }
            }

            return objects;
        }

        public void Update()
        {
            if(Owner == null || Owner.Room == null) 
                return;


            HashSet<GameObject> CuttentObjects = GetherObjects();

            //Spawn 처리 및 despawn 처리

            List<GameObject> added = CuttentObjects.Except(PreviousObjects).ToList();
            if (added.Count > 0)
            {
                S_Spawn spawnPacket = new S_Spawn();
                foreach (GameObject obj in added)
                {
                    ObjectInfo info = new ObjectInfo();
                    info.MergeFrom(obj.Info);

                    spawnPacket.Objects.Add(info);
                }

                Owner.Session.Send(spawnPacket);
            }

            List<GameObject> removed = PreviousObjects.Except(CuttentObjects).ToList();
            if (removed.Count > 0)
            {
                S_Despawn despawnPacket = new S_Despawn();
                foreach (GameObject obj in removed)
                {
                    despawnPacket.ObjectIds.Add(obj.Id);
                }

                Owner.Session.Send(despawnPacket);
            }


            PreviousObjects = CuttentObjects;

            Owner.Room.PushAfter(500, Update);
        }
    }
}
