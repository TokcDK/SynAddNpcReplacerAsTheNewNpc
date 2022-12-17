﻿using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;
using System.Collections.Generic;

namespace SynAddNpcModelReplacerAsTheNewNpc
{
    public enum WorldModelGender
    {
        Any,
        FemaleOnly,
        MaleOnly
    }

    [SynthesisObjectNameMember(nameof(ID))]
    public class NPCReplacerData
    {
        [SynthesisTooltip("Enable using the data")]
        public bool Enabled = true;
        [SynthesisTooltip("Unique Editor ID suffix which will be added for each changed records EDID for this data")]
        public string? ID;
        [SynthesisTooltip("Search string pairs")]
        public HashSet<SearchReplacePair> SearchPairs = new();
        [SynthesisTooltip("Npc specific gender. For case when need to search npcs with specific gender.")]
        public WorldModelGender NpcGender = WorldModelGender.Any;
        [SynthesisTooltip("Url of the web page where the replacer can be downloaded")]
        public string Url = "";
        [SynthesisTooltip("Unique npcs will be ignored")]
        public bool NpcSkipUnique  = true;

        internal Dictionary<FormKey, FormKey> AAReplaceData = new();
    }

    public class SearchReplacePair
    {
        [SynthesisTooltip("Search string of the model path. Subpath in the Meshes dir")]
        public string? SearchWorldModelPath;
        [SynthesisTooltip("Case insensitive search. Enabled by default")]
        public bool CaseInsensitiveSearch = true;
        [SynthesisTooltip("Subpath of the replacer to replace with")]
        public string? ReplaceWith;
    }

    public class Settings
    {
        [SynthesisTooltip("Mods list of changing npc appearance mods")]
        public List<NPCReplacerData> SearchData { get; } = new()
        {
            new NPCReplacerData()
            {
                ID = "PsBossNewDraugrFemale",
                SearchPairs = new()
                {
                    new SearchReplacePair()
                    {
                        SearchWorldModelPath = "Actors\\Draugr\\Character Assets\\HevArmorF_1.nif",
                        ReplaceWith = "PsBoss\\Draugr\\Character Assets\\HevArmorF_1.nif",
                    },
                    new SearchReplacePair()
                    {
                        SearchWorldModelPath = "Actors\\Draugr\\Character Assets\\HevArmorF_2.nif",
                        ReplaceWith = "PsBoss\\Draugr\\Character Assets\\HevArmorF_2.nif",
                    },
                    new SearchReplacePair()
                    {
                        SearchWorldModelPath = "Actors\\Draugr\\Character Assets\\MidArmorF.nif",
                        ReplaceWith = "PsBoss\\Draugr\\Character Assets\\MidArmorF.nif",
                    },
                    new SearchReplacePair()
                    {
                        SearchWorldModelPath = "Actors\\Draugr\\Character Assets\\LightArmorF.nif",
                        ReplaceWith = "PsBoss\\Draugr\\Character Assets\\LightArmorF.nif",
                    },
                    new SearchReplacePair()
                    {
                        SearchWorldModelPath = "Actors\\Draugr\\Character Assets\\DraugrFemale.nif",
                        ReplaceWith = "PsBoss\\Draugr\\Character Assets\\DraugrFemale.nif",
                    },
                },
                NpcGender = WorldModelGender.FemaleOnly,
                Url = "https://www.nexusmods.com/skyrim/mods/114122"
            },
            new NPCReplacerData()
            {
                Enabled = false,
                ID = "PsBossNewDraugrFemaleNude",
                SearchPairs = new()
                {
                    new SearchReplacePair()
                    {
                        SearchWorldModelPath = "Actors\\Draugr\\Character Assets\\HevArmorF_1.nif",
                        ReplaceWith = "PsBossN\\Draugr\\Character Assets\\HevArmorF_1.nif",
                    },
                    new SearchReplacePair()
                    {
                        SearchWorldModelPath = "Actors\\Draugr\\Character Assets\\HevArmorF_2.nif",
                        ReplaceWith = "PsBossN\\Draugr\\Character Assets\\HevArmorF_2.nif",
                    },
                    new SearchReplacePair()
                    {
                        SearchWorldModelPath = "Actors\\Draugr\\Character Assets\\MidArmorF.nif",
                        ReplaceWith = "PsBossN\\Draugr\\Character Assets\\MidArmorF.nif",
                    },
                    new SearchReplacePair()
                    {
                        SearchWorldModelPath = "Actors\\Draugr\\Character Assets\\LightArmorF.nif",
                        ReplaceWith = "PsBossN\\Draugr\\Character Assets\\LightArmorF.nif",
                    },
                    new SearchReplacePair()
                    {
                        SearchWorldModelPath = "Actors\\Draugr\\Character Assets\\DraugrFemale.nif",
                        ReplaceWith = "PsBossN\\Draugr\\Character Assets\\DraugrFemale.nif",
                    },
                },
                NpcGender = WorldModelGender.FemaleOnly,
                Url = "https://www.nexusmods.com/skyrim/mods/114122"
            },
        };
    }
}
