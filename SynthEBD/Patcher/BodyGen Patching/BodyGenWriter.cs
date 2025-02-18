﻿using Mutagen.Bethesda.Plugins;
using System.IO;

namespace SynthEBD;

public class BodyGenWriter
{
    public static void WriteBodyGenOutputs(BodyGenConfigs bodyGenConfigs)
    {
        string templates = CompileTemplateINI(bodyGenConfigs);
        string morphs = CompileMorphsINI(bodyGenConfigs);

        string outputDirPath = Path.Combine(PatcherSettings.Paths.OutputDataFolder, "Meshes", "actors", "character", "BodyGenData", PatcherSettings.General.PatchFileName + ".esp");
        Directory.CreateDirectory(outputDirPath);

        string templatePath = Path.Combine(outputDirPath, "templates.ini");
        string morphsPath = Path.Combine(outputDirPath, "morphs.ini");

        Task.Run(() => PatcherIO.WriteTextFile(templatePath, templates));
        Task.Run(() => PatcherIO.WriteTextFile(morphsPath, morphs));
    }

    private static string CompileTemplateINI(BodyGenConfigs bodyGenConfigs)
    {
        string output = "";
        foreach (var assignment in Patcher.BodyGenTracker.AllChosenMorphsMale)
        {
            var currentConfig = bodyGenConfigs.Male.Where(x => x.Label == assignment.Key).First(); // first instead of single in case user has duplicate configs installed
            var assignedTemplates = currentConfig.Templates.Where(x => assignment.Value.Contains(x.Label));

            foreach (var template in assignedTemplates)
            {
                output += template.Label + "=" + template.Specs + Environment.NewLine; // trailing blank line is required by Bodygen
            }
        }

        foreach (var assignment in Patcher.BodyGenTracker.AllChosenMorphsFemale)
        {
            var currentConfig = bodyGenConfigs.Female.Where(x => x.Label == assignment.Key).First();  // first instead of single in case user has duplicate configs installed
            var assignedTemplates = currentConfig.Templates.Where(x => assignment.Value.Contains(x.Label));

            foreach (var template in assignedTemplates)
            {
                output += template.Label + "=" + template.Specs + Environment.NewLine; // trailing blank line is required by Bodygen
            }
        }

        return output;
    }

    private static string CompileMorphsINI(BodyGenConfigs bodyGenConfigs)
    {
        string output = "";
        foreach (var npcAssignment in Patcher.BodyGenTracker.NPCAssignments)
        {
            output += FormatFormKeyForBodyGen(npcAssignment.Key) + "=";
            for (int i = 0; i < npcAssignment.Value.Count; i++)
            {
                output += npcAssignment.Value[i];
                if (i < npcAssignment.Value.Count - 1)
                {
                    output += ",";
                }
            }
            output += Environment.NewLine; // trailing blank line is required by Bodygen
        }
        return output;
    }

    public static string FormatFormKeyForBodyGen(FormKey FK)
    {
        return FK.ModKey.ToString() + "|" + FK.IDString().TrimStart(new Char[] { '0' });
    }
}