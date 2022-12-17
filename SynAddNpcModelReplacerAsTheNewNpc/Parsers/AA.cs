using System.Collections.ObjectModel;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using static SynAddNpcModelReplacerAsTheNewNpc.Program;

namespace SynAddNpcModelReplacerAsTheNewNpc.Parsers
{
    internal class AAParse
    {
        internal static readonly Dictionary<FormKey, List<TargetFormKeyData>> aaList = new();

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
        }
    }
}