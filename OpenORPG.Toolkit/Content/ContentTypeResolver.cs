﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenORPG.Database.DAL;
using Server.Game.Database;
using Server.Game.Database.Models;
using Server.Game.Database.Models.ContentTemplates;
using Server.Game.Database.Models.Quests;

namespace OpenORPG.Toolkit.Content
{
    /// <summary>
    /// A static utility to resolve template data
    /// </summary>
    public static class ContentTypeResolver
    {

        public static void AddContentWithVirtualCategory(Type type, string category)
        {
            category = category.Replace("\\", "/");

            //TODO: This modules logic sucks

            using (var db = new GameDatabaseContext())
            {
                var monsterRepository = new MonsterRepository(db);
                var itemRepo = new ItemRepository(db);
                var skillRepo = new SkillRepository(db);
                var questRepo = new QuestRepository(db);
                var npcRepo = new NpcRepository(db);

                itemRepo.Get(1);

                string NewName = "New Content";
                var @switch = new Dictionary<Type, Action>
            {                
                  { typeof (MonsterTemplate), () => monsterRepository.Add(new MonsterTemplate() { VirtualCategory = category, Name = NewName } ) },
                  { typeof (ItemTemplate), () => itemRepo.Add(new ItemTemplate() { VirtualCategory = category, Name = NewName }  ) },
                  { typeof (SkillTemplate), () => skillRepo.Add(new SkillTemplate()  { VirtualCategory = category, Name = NewName } ) },     
                  { typeof (QuestTemplate), () => questRepo.Add(new QuestTemplate()  { VirtualCategory = category, Name = NewName } ) },
                  { typeof (NpcTemplate), () => npcRepo.Add(new NpcTemplate()  { VirtualCategory = category, Name = NewName } ) }   
            };

                @switch[type]();

            }


        }

        public static List<IContentTemplate> GetContentTemplateFromType(Type type)
        {
            List<IContentTemplate> result = null;

            using (var db = new GameDatabaseContext())
            {
                var monsterRepository = new MonsterRepository(db);
                var itemRepo = new ItemRepository(db);
                var skillRepo = new SkillRepository(db);
                var questRepo = new QuestRepository(db);
                var npcRepo = new NpcRepository(db);

                itemRepo.Get(1);

                var @switch = new Dictionary<Type, Action>
            {                
                  { typeof (MonsterTemplate), () => result = new List<IContentTemplate>(monsterRepository.GetAll().ToList()) },
                  { typeof (ItemTemplate), () => result = new List<IContentTemplate>(itemRepo.GetAll().ToList()) },
                  { typeof (SkillTemplate), () => result = new List<IContentTemplate>(skillRepo.GetAll().ToList()) },     
                  { typeof (QuestTemplate), () => result = new List<IContentTemplate>(questRepo.GetAll().ToList()) },
                  { typeof (NpcTemplate), () => result = new List<IContentTemplate>(npcRepo.GetAll().ToList()) }   
            };

                @switch[type]();

            }

            return result;
        }




    }
}
