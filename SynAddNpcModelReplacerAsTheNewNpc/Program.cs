using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;

namespace SynAddNpcModelReplacerAsTheNewNpc
{
    public class Program
    {
        static Lazy<Settings> _settings = null!;
        internal static Settings Settings { get => _settings.Value; }

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings("Settings", "settings.json", out _settings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SynAddNpcModelReplacerAsTheNewNpc.esp")
                .Run(args);
        }

        public class TargetFormKeyData
        {
            public FormKey FormKey;
            public NPCReplacerData? Data;
            public SearchReplacePair? Pair;
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var data = Settings.SearchData;

            Console.WriteLine($"Search and modify model paths..");
            var aaList = new Dictionary<FormKey, List<TargetFormKeyData>>();
            foreach (var context in state.LoadOrder.PriorityOrder.ArmorAddon().WinningContextOverrides())
            {
                var getter = context.Record;
                if (getter.WorldModel == null) continue;

                foreach (var target in data)
                {
                    IArmorAddon? aacache = null;

                    foreach((IModelGetter? worldModel, WorldModelGender genderFlag) in new[]
                    {
                        ( getter.WorldModel.Female, WorldModelGender.FemaleOnly ),
                        ( getter.WorldModel.Male, WorldModelGender.MaleOnly )
                    })
                    {
                        if ((target.NpcGender == genderFlag
                            || target.NpcGender == WorldModelGender.Any)
                            && worldModel != null
                            && !worldModel.File.IsNull
                            )
                        {
                            SearchReplacePair? pair = null;
                            foreach (var searchPair in target.SearchPairs)
                            {
                                if (!string.Equals(worldModel.File.RawPath,
                                    searchPair.SearchWorldModelPath, StringComparison.InvariantCultureIgnoreCase)) continue;

                                pair = searchPair;
                                break;
                            }

                            if (pair == null) continue;

                            // create copy of found aa with changed female wmodel path to replacer path
                            var aa = context.DuplicateIntoAsNewRecord(state.PatchMod);

                            var path = worldModel.File.DataRelativePath
                                .Replace(pair.SearchWorldModelPath!, pair.ReplaceWith, StringComparison.InvariantCultureIgnoreCase);

                            Model? tm = genderFlag == WorldModelGender.FemaleOnly ?
                                aa.WorldModel!.Female :
                                aa.WorldModel!.Male;

                            tm!.File.TrySetPath(path);
                            if(aacache == null) aa.EditorID = getter.EditorID + target.EDIDSuffix;

                            var d = new TargetFormKeyData
                            {
                                FormKey = aa.FormKey,
                                Data = target,
                                Pair = pair,
                            };

                            if (!aaList.ContainsKey(getter.FormKey))
                            {
                                aaList.Add(getter.FormKey, new List<TargetFormKeyData>() { d });
                            }
                            else aaList[getter.FormKey].Add(d);

                            aacache = aa;
                        }
                    }
                }
            }
            Console.WriteLine($"Created {aaList.Count} modified skin aa");



            // search all armors referring found aa
            Console.WriteLine($"Process skins to use new models..");
            var aList = new Dictionary<FormKey, TargetFormKeyData>();
            foreach (var context in state.LoadOrder.PriorityOrder.Armor().WinningContextOverrides())
            {
                var getter = context.Record;

                if (!getter.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable)
                    && getter.BodyTemplate != null
                    && !getter.BodyTemplate.Flags.HasFlag(BodyTemplate.Flag.NonPlayable)) continue;
                if (getter.Armature.Count != 1) continue;
                if (aList.ContainsKey(getter.FormKey)) continue;

                var aafKey = getter.Armature[0].FormKey;
                if (!aaList.ContainsKey(aafKey)) continue;

                // create copy of found armors and relink aa there to changed aa
                var changed = context.DuplicateIntoAsNewRecord(state.PatchMod);

                changed.Armature.Clear();
                var aad = aaList[aafKey];
                changed.Armature.Add(aad.FormKey);
                changed.EditorID = getter.EditorID + aad.Data!.EDIDSuffix;

                var d = new TargetFormKeyData
                {
                    FormKey = changed.FormKey,
                    Data = aad.Data,
                    Pair = aad.Pair,
                };

                aList.Add(getter.FormKey, d);
            }
            Console.WriteLine($"Created {aList.Count} modified a skins");

            // search all npc where worn armor is equal found
            Console.WriteLine($"Process npc records to use new skins..");
            var npcList = new Dictionary<FormKey, TargetFormKeyData>();
            foreach (var context in state.LoadOrder.PriorityOrder.Npc().WinningContextOverrides())
            {
                var getter = context.Record;

                if (getter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique)) continue;
                if (getter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Essential)) continue;
                if (getter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Protected)) continue;
                if (!getter.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Female)) continue;
                if (npcList.ContainsKey(getter.FormKey)) continue;

                var wArmrFormKey = GetWornArmorFlag(getter, state);
                if (!aList.ContainsKey(wArmrFormKey)) continue;

                // create copy of npc which to place as extra lnpc recors and relink worn armor to changed
                var changed = context.DuplicateIntoAsNewRecord(state.PatchMod);

                var ad = aList[wArmrFormKey];

                changed.WornArmor.SetTo(ad.FormKey);
                changed.EditorID = getter.EditorID + ad.Data!.EDIDSuffix;

                var d = new TargetFormKeyData
                {
                    FormKey = changed.FormKey,
                    Data = ad.Data,
                    Pair = ad.Pair,
                };

                npcList.Add(getter.FormKey, d);
            }
            Console.WriteLine($"Created {npcList.Count} modified npcss");

            // search npc lists where is found npc placed
            int changedCnt = 0;
            Console.WriteLine($"Process npc lsts for npc records to add..");
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

                    var npcd = npcList[fkey];

                    entriesParsed.Add(e);
                    entries2add.Add(GetLeveledNpcEntrie(npcd.FormKey, e.Data.Level, e.Data.Count));
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

                foreach (var entry in entries2add) changed.Entries!.Add(entry);

                changedCnt++;
            }
            Console.WriteLine($"Changed {changedCnt} leveled npc lists");
        }

        private static LeveledNpcEntry GetLeveledNpcEntrie(FormKey formKey, short level, short count)
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

        private static FormKey GetWornArmorFlag(INpcGetter getter, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!getter.WornArmor.IsNull)
            {
                return getter.WornArmor.FormKey;
            }

            if (!getter.Template.IsNull)
            {
                return GetTemplateFormKey(getter.Template, state);
            }

            return FormKey.Null;
        }

        private static FormKey GetTemplateFormKey(IFormLinkNullableGetter<INpcSpawnGetter> template, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!template.TryResolve(state.LinkCache, out var npc)) return FormKey.Null;

            if (npc is not INpcGetter n) return FormKey.Null;

            if (!n.WornArmor.IsNull)
            {
                return n.WornArmor.FormKey;
            }

            if (!n.Template.IsNull)
            {
                return GetTemplateFormKey(n.Template, state);
            }

            return FormKey.Null;
        }
    }
}
