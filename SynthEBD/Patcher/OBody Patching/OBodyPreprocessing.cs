﻿using Mutagen.Bethesda.Skyrim;

namespace SynthEBD;

public class OBodyPreprocessing
{
    public static void FlattenGroupAttributes(Settings_OBody oBodySettings)
    {
        foreach (var preset in oBodySettings.BodySlidesMale)
        {
            preset.AllowedAttributes = NPCAttribute.SpreadGroupTypeAttributes(preset.AllowedAttributes, oBodySettings.AttributeGroups);
            preset.DisallowedAttributes = NPCAttribute.SpreadGroupTypeAttributes(preset.DisallowedAttributes, oBodySettings.AttributeGroups);
        }
        foreach (var preset in oBodySettings.BodySlidesFemale)
        {
            preset.AllowedAttributes = NPCAttribute.SpreadGroupTypeAttributes(preset.AllowedAttributes, oBodySettings.AttributeGroups);
            preset.DisallowedAttributes = NPCAttribute.SpreadGroupTypeAttributes(preset.DisallowedAttributes, oBodySettings.AttributeGroups);
        }

        foreach (var descriptor in oBodySettings.TemplateDescriptors)
        {
            descriptor.AssociatedRules.AllowedAttributes = NPCAttribute.SpreadGroupTypeAttributes(descriptor.AssociatedRules.AllowedAttributes, oBodySettings.AttributeGroups);
            descriptor.AssociatedRules.DisallowedAttributes = NPCAttribute.SpreadGroupTypeAttributes(descriptor.AssociatedRules.DisallowedAttributes, oBodySettings.AttributeGroups);
        }
    }

    public static void CompilePresetRaces(Settings_OBody oBodySettings)
    {
        foreach (var preset in oBodySettings.BodySlidesMale)
        {
            preset.AllowedRaces = RaceGrouping.MergeRaceAndGroupingList(preset.AllowedRaceGroupings, PatcherSettings.General.RaceGroupings, preset.AllowedRaces);
            preset.DisallowedRaces = RaceGrouping.MergeRaceAndGroupingList(preset.DisallowedRaceGroupings, PatcherSettings.General.RaceGroupings, preset.DisallowedRaces);
        }
        foreach (var preset in oBodySettings.BodySlidesFemale)
        {
            preset.AllowedRaces = RaceGrouping.MergeRaceAndGroupingList(preset.AllowedRaceGroupings, PatcherSettings.General.RaceGroupings, preset.AllowedRaces);
            preset.DisallowedRaces = RaceGrouping.MergeRaceAndGroupingList(preset.DisallowedRaceGroupings, PatcherSettings.General.RaceGroupings, preset.DisallowedRaces);
        }
    }

    public static void CompileRulesRaces(Settings_OBody oBodySettings)
    {
        foreach (var descriptor in oBodySettings.TemplateDescriptors)
        {
            descriptor.AssociatedRules.AllowedRaces = RaceGrouping.MergeRaceAndGroupingList(descriptor.AssociatedRules.AllowedRaceGroupings, PatcherSettings.General.RaceGroupings, descriptor.AssociatedRules.AllowedRaces);
            descriptor.AssociatedRules.DisallowedRaces = RaceGrouping.MergeRaceAndGroupingList(descriptor.AssociatedRules.DisallowedRaceGroupings, PatcherSettings.General.RaceGroupings, descriptor.AssociatedRules.DisallowedRaces);
        }
    }

    public static bool NPCIsEligibleForBodySlide(INpcGetter npc)
    {
        if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.Traits) && npc.Template != null && !npc.Template.FormKey.IsNull)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}