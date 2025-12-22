using UnityEngine;
using System.Collections.Generic;

namespace Combat.TurnBased
{
    public class BattleParty : MonoBehaviour
    {
        [Header("Party Members")]
        public List<BattleUnit> partyMembers = new List<BattleUnit>();
        
        [Header("Formation")]
        public Vector3[] formationPositions;
        
        public void InitializeParty()
        {
            foreach (var member in partyMembers)
            {
                if (member != null)
                    member.Initialize();
            }
        }
        
        public BattleUnit[] GetAliveUnits()
        {
            List<BattleUnit> alive = new List<BattleUnit>();
            foreach (var member in partyMembers)
            {
                if (member != null && member.IsAlive())
                    alive.Add(member);
            }
            return alive.ToArray();
        }
        
        public bool AreAllDead()
        {
            foreach (var member in partyMembers)
            {
                if (member != null && member.IsAlive())
                    return false;
            }
            return true;
        }
        
        public int GetAliveCount()
        {
            int count = 0;
            foreach (var member in partyMembers)
            {
                if (member != null && member.IsAlive())
                    count++;
            }
            return count;
        }
        
        public void AddMember(BattleUnit unit)
        {
            if (!partyMembers.Contains(unit))
                partyMembers.Add(unit);
        }
        
        public void RemoveMember(BattleUnit unit)
        {
            partyMembers.Remove(unit);
        }
        
        public void ClearParty()
        {
            partyMembers.Clear();
        }
    }
}