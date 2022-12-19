using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;

namespace SynAddNpcModelReplacerAsTheNewNpc.Parsers
{
    internal class LNPCParse
    {
        internal static void AddChangedNPC(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (NPCParse.NPCList.Count == 0)
            {
                Console.WriteLine("No npcs was changed..");
                return;
            }

            // search npc lists where is found npc placed
            int changedCnt = 0;
            Console.WriteLine($"Process npc lsts for npc records to add..");
            var npcList = NPCParse.NPCList;
            foreach (var context in state.LoadOrder.PriorityOrder.LeveledNpc().WinningContextOverrides())
            {
                var getter = context.Record;

                if (getter.Entries == null) continue;

                var entries2add = new List<LeveledNpcEntry>();
                var entriesParsed = new HashSet<ILeveledNpcEntryGetter>();

                void ParseEntry(ILeveledNpcEntryGetter e)
                {
                    if (e.Data == null) return;
                    if (e.Data.Reference.IsNull) return;
                    if (entriesParsed.Contains(e)) return;

                    var fkey = e.Data.Reference.FormKey;
                    if (!npcList.ContainsKey(fkey)) return;

                    var npcdlist = npcList[fkey];

                    foreach (var npcd in npcdlist)
                    {
                        entriesParsed.Add(e);
                        entries2add.Add(GetLeveledNpcEntrie(npcd.FormKey, e.Data.Level, e.Data.Count));
                    }
                }

                foreach (var e in getter.Entries) ParseEntry(e);

                // check records from source even ef they are removed by other mod
                if (context.ModKey != getter.FormKey.ModKey
                    && state.LoadOrder.TryGetValue(getter.FormKey.ModKey, out var mod)
                    && mod.Mod!.LeveledNpcs.TryGetValue(getter.FormKey, out var o)
                    && o.Entries != null
                    )
                {
                    foreach (var e in o.Entries) ParseEntry(e);
                }

                if (entries2add.Count == 0) continue;

                // place changed npc records links in lnpc lists
                var changed = state.PatchMod.LeveledNpcs.GetOrAddAsOverride(getter);

                foreach (var entry in entries2add)
                {
                    if (entry.Data!.Reference.FormKey == changed.FormKey) continue;

                    changed.Entries!.Add(entry);
                }

                changedCnt++;
            }
            Console.WriteLine($"Changed {changedCnt} leveled npc lists");
        }

        internal static void AddChangedNPC2(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var patch = state.PatchMod;
            var cache = state.LinkCache;
            var patchModKey = patch.ModKey;
            foreach (var context in state.LoadOrder.PriorityOrder
                .LeveledNpc()
                .WinningContextOverrides()
                .Where(g => g.Record.FormKey.ModKey != patchModKey)
                )
            {
                var getter = context.Record;

                if (getter.Entries == null) continue;

                foreach (var entry in getter.Entries)
                {
                    if(entry.Data==null) continue;
                    if (entry.Data.Reference.IsNull) continue;
                    if (!entry.Data.Reference
                        .TryResolve<INpcGetter>(cache, out var npcGetter)) continue;
                    if (npcGetter.Configuration.TemplateFlags
                        .HasFlag(NpcConfiguration.TemplateFlag.Traits)) continue;

                    if (!RaceParse.RaceList.ContainsKey(npcGetter.Race.FormKey)) continue;


                }
            }
        }

        internal static LeveledNpcEntry GetLeveledNpcEntrie(FormKey formKey, short level = 1, short count = 1)
        {
            var e = new LeveledNpcEntry
            {
                Data = new LeveledNpcEntryData
                {
                    Level = level,
                    Count = count,
                }
            };
            e.Data.Reference.SetTo(formKey);

            return e;
        }
    }
}
