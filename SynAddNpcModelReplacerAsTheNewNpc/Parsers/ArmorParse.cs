using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using static SynAddNpcModelReplacerAsTheNewNpc.Program;

namespace SynAddNpcModelReplacerAsTheNewNpc.Parsers
{
    internal class ArmorParse
    {
        internal static readonly Dictionary<FormKey, List<TargetFormKeyData>> ChangedArmorsList = new();

        internal static void GetChangedSkinArmors(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (AAParse.ChangedSkinAAList.Count == 0)
            {
                Console.WriteLine("No skin model paths was changed. Exit");
                return;
            }

            //var data = Program.Settings.SearchData;

            // search all armors referring found aa
            Console.WriteLine($"Process skins to use new models..");
            var changedSkinAAList = AAParse.ChangedSkinAAList;
            var patchMod= state.PatchMod;
            var patchModKey = patchMod.ModKey;
            foreach (var context in state.LoadOrder.PriorityOrder
                .Armor()
                .WinningContextOverrides()
                .Where(g => g.Record.FormKey.ModKey != patchModKey))
            {
                var getter = context.Record;

                if (!getter.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable)
                    && getter.BodyTemplate != null
                    && !getter.BodyTemplate.Flags.HasFlag(BodyTemplate.Flag.NonPlayable)) continue;
                if (getter.Armature.Count != 1) continue;
                //if (ChangedArmorsList.ContainsKey(getter.FormKey)) continue;

                var aafKey = getter.Armature[0].FormKey;
                if (!changedSkinAAList.ContainsKey(aafKey)) continue;

                var aadlist = changedSkinAAList[aafKey];
                foreach (var aad in aadlist)
                {
                    // create copy of found armors and relink aa there to changed aa
                    var changed = context.DuplicateIntoAsNewRecord(patchMod);

                    changed.Armature.Clear();
                    changed.Armature.Add(aad.FormKey);
                    changed.EditorID = getter.EditorID + aad.Data!.ID;

                    var d = new TargetFormKeyData
                    {
                        FormKey = changed.FormKey,
                        Data = aad.Data,
                        Pair = aad.Pair,
                        Object = changed
                    };

                    aad.IsChanged = true;

                    if (!ChangedArmorsList.ContainsKey(getter.FormKey))
                    {
                        ChangedArmorsList.Add(getter.FormKey, new List<TargetFormKeyData>() { d });
                    }
                    else ChangedArmorsList[getter.FormKey].Add(d);
                }
            }
            Console.WriteLine($"Created {ChangedArmorsList.Count} modified a skins");
        }

        internal static void RelinkRaces(Mutagen.Bethesda.Synthesis.IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (RaceParse.RaceList.Count == 0)
            {
                Console.WriteLine("No races was changed..");
                return;
            }

            Console.WriteLine("Relink armor skin races..");
            var patchMod = state.PatchMod;
            var races = RaceParse.RaceList;
            int relinkedCount = 0;
            foreach (var data in ChangedArmorsList)
            {
                List<TargetFormKeyData> add = new();
                foreach (var changed in data.Value)
                {
                    if (changed.Object is not Armor a) continue;
                    if (a.Race == null) continue;
                    var fkey = a.Race.FormKey;
                    if (!races.ContainsKey(fkey)) continue;

                    var rdlist = races[fkey];

                    bool isSet = false;
                    foreach (var rd in rdlist)
                    {
                        if (isSet)
                        {
                            var newa = patchMod.Armors.DuplicateInAsNewRecord(a);

                            var d = new TargetFormKeyData
                            {
                                FormKey = newa.FormKey,
                                Data = rd.Data,
                                Pair = rd.Pair,
                                Object = a
                            };

                            add.Add(d);
                        }
                        else
                        {
                            a.Race.SetTo(rd.FormKey);
                            isSet = true;

                            relinkedCount++;
                        }
                    }
                }

                if (add.Count == 0) continue;

                foreach (var d in add) data.Value.Add(d);
            }
            Console.WriteLine($"Relinked {relinkedCount} armor skin races..");
        }
    }
}