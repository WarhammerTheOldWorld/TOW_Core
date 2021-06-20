﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TOW_Core.CampaignMode
{
    public class PartyAttribute
    {
        [SaveableField(1)]
        public Hero Leader;
        [SaveableField(2)]
        public string id;
        [SaveableField(3)]
        public float WindsOfMagic;
        [SaveableField(4)]
        public bool MagicUserParty;
        [SaveableField(5)]
        public StaticAttribute LeaderAttribute;
        [SaveableField(6)]
        public List<StaticAttribute> CompanionAttributes = new List<StaticAttribute>();
        [SaveableField(7)]
        public List<StaticAttribute> RegularTroopAttributes = new List<StaticAttribute>();

        [SaveableField(8)] public PartyType PartyType;
        [SaveableField(9)] public int numberOfRegularTroops;
        
        public PartyAttribute(string id)
        {
            this.id = id;
        }

        public PartyAttribute()
        {

        }
    }
    
    
    public class PartyAttributeDefiner : SaveableTypeDefiner
    {
        public PartyAttributeDefiner() : base(1_543_132) { }
        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();
            AddClassDefinition(typeof(PartyAttribute), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();
            ConstructContainerDefinition(typeof(Dictionary<string, PartyAttribute>));
        }
    }
    
    
    public enum PartyType{
        [SaveableField(1)]RogueParty,
        [SaveableField(2)]LordParty,
        [SaveableField(3)]Regular,
    }

    public class PartyTypeDefiner : SaveableTypeDefiner
    {
        public PartyTypeDefiner() : base(1_543_134)
        {
            
        }
        protected override void DefineEnumTypes()
        {
            base.DefineEnumTypes();
            AddEnumDefinition(typeof(PartyType), 1);
        }
        
    }
}