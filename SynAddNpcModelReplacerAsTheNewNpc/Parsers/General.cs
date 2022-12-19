namespace SynAddNpcModelReplacerAsTheNewNpc.Parsers
{
    internal class General
    {
        internal static void CleanNotUsing(Mutagen.Bethesda.Synthesis.IPatcherState<Mutagen.Bethesda.Skyrim.ISkyrimMod, Mutagen.Bethesda.Skyrim.ISkyrimModGetter> state)
        {
            Console.WriteLine($"Clean not using data..");

            var aas = state.PatchMod.ArmorAddons;
            foreach (var d in AAParse.ChangedSkinAAList)
            {
                foreach (var dd in d.Value)
                {
                    if (dd.IsChanged) continue;
                    if (!aas.ContainsKey(dd.FormKey)) continue;

                    aas.Remove(dd.FormKey);
                }
            }

            var armors = state.PatchMod.Armors;
            foreach (var d in ArmorParse.ChangedArmorsList)
            {
                foreach (var dd in d.Value)
                {
                    if (dd.IsChanged) continue;
                    if (!armors.ContainsKey(dd.FormKey)) continue;

                    armors.Remove(dd.FormKey);
                }
            }

            var races = state.PatchMod.Races;
            foreach (var d in RaceParse.RaceList)
            {
                foreach (var dd in d.Value)
                {
                    if (dd.IsChanged) continue;
                    if (!races.ContainsKey(dd.FormKey)) continue;

                    races.Remove(dd.FormKey);
                }
            }
        }

        internal static void ShowTargetsInfo()
        {
            Console.WriteLine($"\nUsed target replacers:");
            Console.WriteLine($"---");
            foreach (var target in Program.Settings.SearchData)
            {
                if (!target.Enabled) continue;

                var url = !string.IsNullOrWhiteSpace(target.Url) ? "\n Url: " + target.Url : "";
                var note = !string.IsNullOrWhiteSpace(target.Note) ? "\n Note: " + target.Note : "";
                Console.WriteLine($" ID: {target.ID}{url}{note}");
                Console.WriteLine($"-");
            }
            Console.WriteLine($"---\n");
        }
    }
}