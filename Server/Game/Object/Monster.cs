using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Server.Game
{
    public class Monster : GameObject
    {
        public int TemplateId { get; private set; }
        public Monster()
        {
            ObjectType = GameObjectType.Monster;          
        }

        public void Init(int templatedId)
        {
            TemplateId = templatedId;

            MonsterData monsterData = null;
            DataManager.MonsterDict.TryGetValue(TemplateId, out monsterData);

            if (monsterData == null)
                return;

            Stat.MergeFrom(monsterData.stat);
            Stat.Hp = monsterData.stat.Hp;
            State = CreatureState.Idle;
        }

        IJob _job;
        public override void Update()
        {
            switch (State)
            {
                case CreatureState.Idle:
                    UpdateIdle();
                    break;
                case CreatureState.Moving:
                    UpdateMoving();
                    break;
                case CreatureState.Skill:
                    UpdateSkill();
                    break;
                case CreatureState.Dead:
                    UpdateDead();
                    break;
            }

            if (Room != null)
            {
                _job = Room.PushAfter(200, Update);
            }
        }

        Player _target;
        int _searchCellDist = 10;
        int _chaseCellDist = 20;

        long _nextSearchTick = 0;
        protected virtual void UpdateIdle()
        {
            if (_nextSearchTick > Environment.TickCount64)
                return;
            _nextSearchTick = Environment.TickCount64 + 1000;

            Player target = Room.FindClosestPlayer(CellPos, _searchCellDist);

            if (target == null)
                return;

            _target = target;
            State = CreatureState.Moving;
        }

        int _skillRange = 1;
        long _nextMoveTick = 0;
        protected virtual void UpdateMoving()
        {
            if (_nextMoveTick > Environment.TickCount64)
                return;
            int moveTick = (int)(1000 / Speed);
            _nextMoveTick = Environment.TickCount64 + moveTick;

            if (_target == null || _target.Room != Room)
            {
                _target = null;
                State = CreatureState.Idle;
                BroadcastMove();
                return;
            }

            Vector2Int dir = _target.CellPos - CellPos;
            int dist = dir.cellDistFromZero;
            if (dist == 0 || dist > _chaseCellDist)
            {
                _target = null;
                State = CreatureState.Idle;
                BroadcastMove();
                return;
            }

            List<Vector2Int> path = Room.Map.FindPath(CellPos, _target.CellPos, true);
            if (path.Count < 2 || path.Count > _chaseCellDist)
            {
                _target = null;
                State = CreatureState.Idle;
                BroadcastMove();
                return;
            }

            // 스킬로 넘어갈지 체크
            if (dist <= _skillRange && (dir.x == 0 || dir.y == 0))
            {
                _coolTick = 0;
                State = CreatureState.Skill;
                return;
            }

            // 이동
            Dir = GetDirFromVec(path[1] - CellPos);
            Room.Map.ApplyMove(this, path[1]);
            BroadcastMove();
        }

        void BroadcastMove()
        {
            // 다른 플레이어한테도 알려준다
            S_Move movePacket = new S_Move();
            movePacket.ObjectId = Id;
            movePacket.PosInfo = PosInfo;
            Room.Broadcast(CellPos, movePacket);
        }

        long _coolTick = 0;
        protected virtual void UpdateSkill()
        {
            if (_coolTick == 0)
            {
                // 유효한 타겟인지
                if (_target == null || _target.Room != Room || _target.Hp == 0)
                {
                    _target = null;
                    State = CreatureState.Moving;
                    BroadcastMove();
                    return;
                }

                // 스킬이 아직 사용 가능한지
                Vector2Int dir = (_target.CellPos - CellPos);
                int dist = dir.cellDistFromZero;
                bool canUseSkill = (dist <= _skillRange && (dir.x == 0 || dir.y == 0));
                if (canUseSkill == false)
                {
                    State = CreatureState.Moving;
                    BroadcastMove();
                    return;
                }

                // 타게팅 방향 주시
                MoveDir lookDir = GetDirFromVec(dir);
                if (Dir != lookDir)
                {
                    Dir = lookDir;
                    BroadcastMove();
                }

                Skill skillData = null;
                DataManager.SkillDict.TryGetValue(1, out skillData);

                // 데미지 판정
                _target.OnDamaged(this, skillData.damage + TotalAttack);

                // 스킬 사용 Broadcast
                S_Skill skill = new S_Skill() { Info = new SkillInfo() };
                skill.ObjectId = Id;
                skill.Info.SkillId = skillData.id;
                Room.Broadcast(CellPos, skill);

                // 스킬 쿨타임 적용
                int coolTick = (int)(1000 * skillData.cooldown);
                _coolTick = Environment.TickCount64 + coolTick;
            }

            if (_coolTick > Environment.TickCount64)
                return;

            _coolTick = 0;
        }

        protected virtual void UpdateDead()
        {

        }

        public override void OnDead(GameObject attacker)
        {
            base.OnDead(attacker);

            // 몬스터가 죽을 경우 아이템 생성해보자
            // 막타친애가 누구인가가 중요하다

            GameObject owner =  attacker.GetOwner();

            if (owner.ObjectType == GameObjectType.Player)
            {
                RewardData rewardData = GetRandomReward();

                if (rewardData != null)
                {
                    //아이템이 드랍됐다면!
                    Player player = (Player)owner;

                    Server.DB.DbTransaction.RewardPlayer(player, rewardData, Room);
                }
            }

            //혹시 일감이 남아있다면 실행되지않도록 설정한다.
            if (_job != null)
            {
                _job.Cancel = true;
                _job = null;
            }

        }

        RewardData GetRandomReward()
        {
            MonsterData monsterData = null;
            DataManager.MonsterDict.TryGetValue(TemplateId, out monsterData);

            if (monsterData == null)
                return null;


            int rand = new Random().Next(0, 101);
            int sum = 0;
            foreach (RewardData rewardData in monsterData.rewards)
            {
                sum += rewardData.probability;
                if (rand <= sum)
                {
                    return rewardData;
                }
            }

            return null;            
        }
    }
}
