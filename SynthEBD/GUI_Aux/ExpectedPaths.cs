﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SynthEBD
{
    public class Paths
    {
        private static string SynthEBDexePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public string GeneralSettingsPath = Path.Combine(SynthEBDexePath, "Settings\\GeneralSettings.json");

        public Paths(bool loadFromGameData)
        {
            // create relevant paths if necessary - only in the "home" directory. To avoid inadvertent clutter in the data folder, user must create these directories manually in their data folder
            string settingsDirRelPath = "Settings";
            string assetsDirRelPath = "Asset Packs";
            string heightsDirRelPath = "Height Configurations";
            string bodyGenDirRelPath = "BodyGen Configuration";
            string NPCConfigDirRelPath = "NPC Configuration";
            string recordTemplatesDirRelPath = "Record Templates";

            string settingsDirPath = Path.Combine(SynthEBDexePath, settingsDirRelPath);
            string assetsDirPath = Path.Combine(SynthEBDexePath, assetsDirRelPath);
            string heightsDirPath = Path.Combine(SynthEBDexePath, heightsDirRelPath);
            string bodyGenDirPath = Path.Combine(SynthEBDexePath, bodyGenDirRelPath);
            string NPCConfigDirPath = Path.Combine(SynthEBDexePath, NPCConfigDirRelPath);
            string recordTemplatesDirPath = Path.Combine(SynthEBDexePath, recordTemplatesDirRelPath);

            if (Directory.Exists(settingsDirPath) == false)
            {
                Directory.CreateDirectory(settingsDirPath);
            }
            if (Directory.Exists(assetsDirPath) == false)
            {
                Directory.CreateDirectory(assetsDirPath);
            }
            if (Directory.Exists(heightsDirPath) == false)
            {
                Directory.CreateDirectory(heightsDirPath);
            }
            if (Directory.Exists(bodyGenDirPath) == false)
            {
                Directory.CreateDirectory(bodyGenDirPath);
            }
            if (Directory.Exists(NPCConfigDirPath) == false)
            {
                Directory.CreateDirectory(NPCConfigDirPath);
            }
            if (Directory.Exists(recordTemplatesDirPath) == false)
            {
                Directory.CreateDirectory(recordTemplatesDirPath);
            }

            switch (loadFromGameData)
            {
                case false:
                    RelativePath = SynthEBDexePath;
                    break;
                case true:
                    RelativePath = GameEnvironmentProvider.MyEnvironment.DataFolderPath;
                    break;
            }

            this.TexMeshSettingsPath = Path.Combine(RelativePath, settingsDirRelPath, "TexMeshSettings.json");
            this.AssetPackDirPath = Path.Combine(RelativePath, assetsDirRelPath);
            this.HeightSettingsPath = Path.Combine(RelativePath, settingsDirRelPath, "HeightSettings.json");
            this.HeightConfigDirPath= Path.Combine(RelativePath, heightsDirRelPath);
            this.BodyGenSettingsPath = Path.Combine(RelativePath, settingsDirRelPath, "BodyGenSettings.json");
            this.BodyGenConfigDirPath = Path.Combine(RelativePath, bodyGenDirRelPath);
            this.SpecificNPCAssignmentsPath = Path.Combine(RelativePath, NPCConfigDirRelPath, "Specific NPC Assignments.json");
            this.BlockListPath = Path.Combine(RelativePath, NPCConfigDirRelPath, "BlockList.json");
            this.LinkedNPCNameExclusionsPath = Path.Combine(RelativePath, settingsDirRelPath, "LinkedNPCNameExclusions.json");
            this.LinkedNPCsPath = Path.Combine(RelativePath, settingsDirRelPath, "LinkedNPCs.json");
            this.TrimPathsPath = Path.Combine(RelativePath, settingsDirRelPath, "TrimPathsByExtension.json");
            this.RecordTemplatesDirPath = Path.Combine(RelativePath, recordTemplatesDirRelPath);

            this.FallBackTexMeshSettingsPath = Path.Combine(SynthEBDexePath, settingsDirPath, "TexMeshSettings.json");
            this.FallBackAssetPackDirPath = Path.Combine(SynthEBDexePath, assetsDirPath);
            this.FallBackHeightSettingsPath = Path.Combine(SynthEBDexePath, settingsDirPath, "HeightSettings.json");
            this.FallBackHeightConfigDirPath = Path.Combine(SynthEBDexePath, heightsDirPath);
            this.FallBackHeightConfigCurrentPath = Path.Combine(this.FallBackHeightConfigDirPath, "HeightConfig.json");
            this.FallBackBodyGenSettingsPath = Path.Combine(SynthEBDexePath, settingsDirPath, "BodyGenSettings.json");
            this.FallBackBodyGenConfigDirPath = Path.Combine(SynthEBDexePath, bodyGenDirPath);
            this.FallBackSpecificNPCAssignmentsPath = Path.Combine(SynthEBDexePath, NPCConfigDirPath, "Specific NPC Assignments.json");
            this.FallBackBlockListPath = Path.Combine(SynthEBDexePath, NPCConfigDirPath, "BlockList.json");
            this.FallBackLinkedNPCNameExclusionsPath = Path.Combine(SynthEBDexePath, settingsDirPath, "LinkedNPCNameExclusions.json");
            this.FallBackLinkedNPCsPath = Path.Combine(SynthEBDexePath, settingsDirPath, "LinkedNPCs.json");
            this.FallBackTrimPathsPath = Path.Combine(SynthEBDexePath, settingsDirPath, "TrimPathsByExtension.json");
            this.FallBackRecordTemplatesDirPath = Path.Combine(SynthEBDexePath, recordTemplatesDirRelPath);
        }

        private string RelativePath { get; set; } 
        
        public string TexMeshSettingsPath { get; set; } // path of the Textures and Meshes settings file
        public string AssetPackDirPath { get; set; }
        public string HeightSettingsPath { get; set; } // path of the Textures and Meshes settings file
        public string HeightConfigDirPath { get; set; }
        public string BodyGenSettingsPath { get; set; }
        public string BodyGenConfigDirPath { get; set; }
        public string SpecificNPCAssignmentsPath { get; set; }
        public string BlockListPath { get; set; }

        public string LinkedNPCNameExclusionsPath { get; set; }
        public string LinkedNPCsPath { get; set; }
        public string TrimPathsPath { get; set; }

        public string RecordTemplatesDirPath { get; set; }

        public string FallBackTexMeshSettingsPath { get; set; } // path of the Textures and Meshes settings file
        public string FallBackAssetPackDirPath { get; set; }
        public string FallBackHeightSettingsPath { get; set; } // path of the Textures and Meshes settings file
        public string FallBackHeightConfigDirPath { get; set; }
        public string FallBackHeightConfigCurrentPath { get; set; }
        public string FallBackBodyGenSettingsPath { get; set; }
        public string FallBackBodyGenConfigDirPath { get; set; }
        public string FallBackSpecificNPCAssignmentsPath { get; set; }
        public string FallBackBlockListPath { get; set; }

        public string FallBackLinkedNPCNameExclusionsPath { get; set; }
        public string FallBackLinkedNPCsPath { get; set; }
        public string FallBackTrimPathsPath { get; set; }

        public string FallBackRecordTemplatesDirPath { get; set; }
    }
}

    
