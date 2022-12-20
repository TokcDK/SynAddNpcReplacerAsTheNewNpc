using System.Diagnostics.Metrics;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using static SynAddNpcModelReplacerAsTheNewNpc.Program;
using Mutagen.Bethesda;

namespace SynAddNpcModelReplacerAsTheNewNpc.Parsers
{
    internal class AAParse
    {
        internal static readonly Dictionary<FormKey, List<TargetFormKeyData>> ChangedSkinAAList = new();

        internal static void GetChangedAAList(Mutagen.Bethesda.Synthesis.IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var data = Program.Settings.SearchData;

            Console.WriteLine($"Search and modify model paths..");
            foreach (var context in state.LoadOrder.PriorityOrder.ArmorAddon().WinningContextOverrides())
            {
                var getter = context.Record;
                if (getter.WorldModel == null) continue;

                foreach (var target in data)
                {
                    if (!target.Enabled) continue;

                    IArmorAddon? aacache = null;
                    foreach ((IModelGetter? worldModel, WorldModelGender genderFlag) in new[]
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
                            if (aacache == null) aa.EditorID = getter.EditorID + target.ID;
                            tm.AlternateTextures = null;

                            var d = new TargetFormKeyData
                            {
                                FormKey = aa.FormKey,
                                Data = target,
                                Pair = pair,
                                Object = aa
                            };

                            if (!ChangedSkinAAList.ContainsKey(getter.FormKey))
                            {
                                ChangedSkinAAList.Add(getter.FormKey, new List<TargetFormKeyData>() { d });
                            }
                            else ChangedSkinAAList[getter.FormKey].Add(d);

                            aacache = aa;
                        }
                    }
                }
            }
            Console.WriteLine($"Created {ChangedSkinAAList.Count} modified skin aa");
        }

        internal static void RelinkRaces(Mutagen.Bethesda.Synthesis.IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {

            if (RaceParse.RaceList.Count == 0)
            {
                Console.WriteLine("No races was changed..");
                return;
            }

            Console.WriteLine("Relink AA races..");
            var patchMod = state.PatchMod;
            var races = RaceParse.RaceList;
            int relinkedCount = 0;
            foreach (var data in ChangedSkinAAList)
            {
                List<TargetFormKeyData> add = new();
                foreach (var changed in data.Value)
                {
                    if (changed.Object is not ArmorAddon aa) continue;
                    if (aa.Race == null) continue;
                    var fkey = aa.Race.FormKey;
                    if (!races.ContainsKey(fkey)) continue;

                    var rdlist = races[fkey];

                    bool isSet = false;
                    foreach(var rd in rdlist)
                    {
                        if (isSet)
                        {
                            var newaa = patchMod.ArmorAddons.DuplicateInAsNewRecord(aa);

                            var d = new TargetFormKeyData
                            {
                                FormKey = newaa.FormKey,
                                Data = rd.Data,
                                Pair = rd.Pair,
                                Object = aa
                            };

                            add.Add(d);
                        }
                        else
                        {
                            aa.Race.SetTo(rd.FormKey);
                            isSet = true;
                        }
                    }
                }

                if (add.Count == 0) continue;

                foreach(var d in add) data.Value.Add(d);

                relinkedCount++;
            }
            Console.WriteLine($"Relinked {relinkedCount} AA races..");
        }
    }
}