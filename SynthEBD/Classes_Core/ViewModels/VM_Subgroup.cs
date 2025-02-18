using System.Collections.ObjectModel;
using System.ComponentModel;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Specialized;
using ReactiveUI;
using GongSolutions.Wpf.DragDrop;
using DynamicData.Binding;
using System.Text.RegularExpressions;

namespace SynthEBD;

public class VM_Subgroup : VM, ICloneable, IDropTarget, IHasSubgroupViewModels
{
    private readonly VM_SettingsOBody _oBody;
    private readonly Factory _selfFactory;
    private readonly VM_FilePathReplacement.Factory _filePathReplacementFactory;

    public delegate VM_Subgroup Factory(
        ObservableCollection<VM_RaceGrouping> raceGroupingVMs,
        ObservableCollection<VM_Subgroup> parentCollection,
        VM_AssetPack parentAssetPack,
        VM_Subgroup parentSubgroup,
        bool setExplicitReferenceNPC);
    
    public VM_Subgroup(
        ObservableCollection<VM_RaceGrouping> raceGroupingVMs,
        ObservableCollection<VM_Subgroup> parentCollection,
        VM_AssetPack parentAssetPack, 
        VM_Subgroup parentSubgroup,
        bool setExplicitReferenceNPC,
        VM_SettingsOBody oBody,
        Factory selfFactory,
        VM_FilePathReplacement.Factory filePathReplacementFactory)
    {
        _oBody = oBody;
        _selfFactory = selfFactory;
        _filePathReplacementFactory = filePathReplacementFactory;
        SubscribedRaceGroupings = raceGroupingVMs;
        SetExplicitReferenceNPC = setExplicitReferenceNPC;
        ParentAssetPack = parentAssetPack;

        this.AllowedRaceGroupings = new VM_RaceGroupingCheckboxList(SubscribedRaceGroupings);
        this.DisallowedRaceGroupings = new VM_RaceGroupingCheckboxList(SubscribedRaceGroupings);
        if (parentAssetPack.TrackedBodyGenConfig != null)
        {
            this.AllowedBodyGenDescriptors = new VM_BodyShapeDescriptorSelectionMenu(parentAssetPack.TrackedBodyGenConfig.DescriptorUI, SubscribedRaceGroupings, parentAssetPack);
            this.DisallowedBodyGenDescriptors = new VM_BodyShapeDescriptorSelectionMenu(parentAssetPack.TrackedBodyGenConfig.DescriptorUI, SubscribedRaceGroupings, parentAssetPack);
        }
        AllowedBodySlideDescriptors = new VM_BodyShapeDescriptorSelectionMenu(oBody.DescriptorUI, SubscribedRaceGroupings, parentAssetPack);
        DisallowedBodySlideDescriptors = new VM_BodyShapeDescriptorSelectionMenu(oBody.DescriptorUI, SubscribedRaceGroupings, parentAssetPack);

        PatcherEnvironmentProvider.Instance.WhenAnyValue(x => x.Environment.LinkCache)
            .Subscribe(x => LinkCache = x)
            .DisposeWith(this);
        
        //UI-related
        this.ParentCollection = parentCollection;
        this.ParentSubgroup = parentSubgroup;
        this.RequiredSubgroups.ToObservableChangeSet().Subscribe(x => RefreshListBoxLabel(RequiredSubgroups, SubgroupListBox.Required));
        this.ExcludedSubgroups.ToObservableChangeSet().Subscribe(x => RefreshListBoxLabel(ExcludedSubgroups, SubgroupListBox.Excluded));

        // must be set after Parent Asset Pack
        if (SetExplicitReferenceNPC)
        {
            this.PathsMenu = new VM_FilePathReplacementMenu(this, SetExplicitReferenceNPC, this.LinkCache);
        }
        else
        {
            this.PathsMenu = new VM_FilePathReplacementMenu(this, SetExplicitReferenceNPC, parentAssetPack.RecordTemplateLinkCache);
            parentAssetPack.WhenAnyValue(x => x.RecordTemplateLinkCache).Subscribe(x => this.PathsMenu.ReferenceLinkCache = parentAssetPack.RecordTemplateLinkCache);
        }

        this.PathsMenu.Paths.ToObservableChangeSet().Subscribe(x => GetDDSPaths(ImagePaths));
        this.WhenAnyValue(x => x.ParentAssetPack, x => x.ParentSubgroup).Subscribe(x => GetDDSPaths(ImagePaths));

        AutoGenerateID();

        AutoGenerateIDcommand = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => AutoGenerateID()
        );

        AutoGenerateID_Children_Command = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => AutoGenerateID_Children()
        );

        AutoGenerateID_All_Command = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => ParentAssetPack.AutoGenerateSubgroupIDs()
        );

        AddAllowedAttribute = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.AllowedAttributes.Add(VM_NPCAttribute.CreateNewFromUI(this.AllowedAttributes, true, null, parentAssetPack.AttributeGroupMenu.Groups))
        );

        AddDisallowedAttribute = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.DisallowedAttributes.Add(VM_NPCAttribute.CreateNewFromUI(this.DisallowedAttributes, false, null, parentAssetPack.AttributeGroupMenu.Groups))
        );

        AddNPCKeyword = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.AddKeywords.Add(new VM_CollectionMemberString("", this.AddKeywords))
        );

        AddPath = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.PathsMenu.Paths.Add(filePathReplacementFactory(this.PathsMenu))
        );

        AddSubgroup = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.Subgroups.Add(selfFactory(raceGroupingVMs, this.Subgroups, this.ParentAssetPack, this, setExplicitReferenceNPC))
        );

        DeleteMe = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.ParentCollection.Remove(this)
        );

        DeleteRequiredSubgroup = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute:  x => { this.RequiredSubgroups.Remove((VM_Subgroup)x); RefreshListBoxLabel(this.RequiredSubgroups, SubgroupListBox.Required); }
        );

        DeleteExcludedSubgroup = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: x => { this.ExcludedSubgroups.Remove((VM_Subgroup)x); RefreshListBoxLabel(this.ExcludedSubgroups, SubgroupListBox.Excluded); }
        );
    }

    public string ID { get; set; } = "";
    public string Name { get; set; } = "New";
    public bool Enabled { get; set; } = true;
    public bool DistributionEnabled { get; set; } = true;
    public ObservableCollection<FormKey> AllowedRaces { get; set; } = new();
    public VM_RaceGroupingCheckboxList AllowedRaceGroupings { get; set; }
    public ObservableCollection<FormKey> DisallowedRaces { get; set; } = new();
    public VM_RaceGroupingCheckboxList DisallowedRaceGroupings { get; set; }
    public ObservableCollection<VM_NPCAttribute> AllowedAttributes { get; set; } = new();
    public ObservableCollection<VM_NPCAttribute> DisallowedAttributes { get; set; } = new();
    public bool AllowUnique { get; set; } = true;
    public bool AllowNonUnique { get; set; } = true;
    public ObservableCollection<VM_Subgroup> RequiredSubgroups { get; set; } = new();
    public ObservableCollection<VM_Subgroup> ExcludedSubgroups { get; set; } = new();
    public ObservableCollection<VM_CollectionMemberString> AddKeywords { get; set; } = new();
    public double ProbabilityWeighting { get; set; } = 1;
    public VM_FilePathReplacementMenu PathsMenu { get; set; }
    public VM_BodyShapeDescriptorSelectionMenu AllowedBodyGenDescriptors { get; set; }
    public VM_BodyShapeDescriptorSelectionMenu DisallowedBodyGenDescriptors { get; set; }
    public VM_BodyShapeDescriptorSelectionMenu AllowedBodySlideDescriptors { get; set; }
    public VM_BodyShapeDescriptorSelectionMenu DisallowedBodySlideDescriptors { get; set; }
    public NPCWeightRange WeightRange { get; set; } = new();
    public ObservableCollection<VM_Subgroup> Subgroups { get; set; } = new();

    //UI-related
    public ILinkCache LinkCache { get; private set; }
    public IEnumerable<Type> RacePickerFormKeys { get; set; } = typeof(IRaceGetter).AsEnumerable();

    public string TopLevelSubgroupID { get; set; }
    public RelayCommand AutoGenerateIDcommand { get; }
    public RelayCommand AutoGenerateID_Children_Command { get; }
    public RelayCommand AutoGenerateID_All_Command { get; }
    public RelayCommand AddAllowedAttribute { get; }
    public RelayCommand AddDisallowedAttribute { get; }
    public RelayCommand AddNPCKeyword { get; }
    public RelayCommand AddPath { get; }
    public RelayCommand AddSubgroup { get; }
    public RelayCommand DeleteMe { get; }
    public RelayCommand DeleteRequiredSubgroup { get; }
    public RelayCommand DeleteExcludedSubgroup { get; }
    public bool SetExplicitReferenceNPC { get; set; }
    public HashSet<string> RequiredSubgroupIDs { get; set; } = new(); // temporary placeholder for RequiredSubgroups until all subgroups are loaded in
    public HashSet<string> ExcludedSubgroupIDs { get; set; } = new(); // temporary placeholder for ExcludedSubgroups until all subgroups are loaded in
    public string RequiredSubgroupsLabel { get; set; } = "Drag subgroups here from the tree view";
    public string ExcludedSubgroupsLabel { get; set; } = "Drag subgroups here from the tree view";

    public ObservableCollection<VM_Subgroup> ParentCollection { get; set; }
    public VM_AssetPack ParentAssetPack { get; set; }
    public VM_Subgroup ParentSubgroup { get; set; }
    public ObservableCollection<VM_RaceGrouping> SubscribedRaceGroupings { get; set; }
    public ObservableCollection<ImagePreviewHandler.ImagePathWithSource> ImagePaths { get; set; } = new();

    public void CopyInViewModelFromModel(
        AssetPack.Subgroup model,
        VM_Settings_General generalSettingsVM)
    {
        ID = model.ID;
        Name = model.Name;
        Enabled = model.Enabled;
        DistributionEnabled = model.DistributionEnabled;
        AllowedRaces = new ObservableCollection<FormKey>(model.AllowedRaces);
        AllowedRaceGroupings = VM_RaceGroupingCheckboxList.GetRaceGroupingsByLabel(model.AllowedRaceGroupings, generalSettingsVM.RaceGroupings);
        DisallowedRaces = new ObservableCollection<FormKey>(model.DisallowedRaces);
        DisallowedRaceGroupings = VM_RaceGroupingCheckboxList.GetRaceGroupingsByLabel(model.DisallowedRaceGroupings, generalSettingsVM.RaceGroupings);
        AllowedAttributes = VM_NPCAttribute.GetViewModelsFromModels(model.AllowedAttributes, ParentAssetPack.AttributeGroupMenu.Groups, true, null);
        DisallowedAttributes = VM_NPCAttribute.GetViewModelsFromModels(model.DisallowedAttributes, ParentAssetPack.AttributeGroupMenu.Groups, false, null);
        foreach (var x in DisallowedAttributes) { x.DisplayForceIfOption = false; }
        AllowUnique = model.AllowUnique;
        AllowNonUnique = model.AllowNonUnique;
        RequiredSubgroupIDs = model.RequiredSubgroups;
        RequiredSubgroups = new ObservableCollection<VM_Subgroup>();
        ExcludedSubgroupIDs = model.ExcludedSubgroups;
        ExcludedSubgroups = new ObservableCollection<VM_Subgroup>();
        AddKeywords = VM_CollectionMemberString.InitializeObservableCollectionFromICollection(model.AddKeywords);
        ProbabilityWeighting = model.ProbabilityWeighting;
        PathsMenu = VM_FilePathReplacementMenu.GetViewModelFromModels(model.Paths, this, SetExplicitReferenceNPC, _filePathReplacementFactory);
        WeightRange = model.WeightRange;

        if (ParentAssetPack.TrackedBodyGenConfig != null)
        {
            AllowedBodyGenDescriptors = VM_BodyShapeDescriptorSelectionMenu.InitializeFromHashSet(model.AllowedBodyGenDescriptors, ParentAssetPack.TrackedBodyGenConfig.DescriptorUI, SubscribedRaceGroupings, ParentAssetPack);
            DisallowedBodyGenDescriptors = VM_BodyShapeDescriptorSelectionMenu.InitializeFromHashSet(model.DisallowedBodyGenDescriptors, ParentAssetPack.TrackedBodyGenConfig.DescriptorUI, SubscribedRaceGroupings, ParentAssetPack);
        }

        AllowedBodySlideDescriptors = VM_BodyShapeDescriptorSelectionMenu.InitializeFromHashSet(model.AllowedBodySlideDescriptors, _oBody.DescriptorUI, SubscribedRaceGroupings, ParentAssetPack);
        DisallowedBodySlideDescriptors = VM_BodyShapeDescriptorSelectionMenu.InitializeFromHashSet(model.DisallowedBodySlideDescriptors, _oBody.DescriptorUI, SubscribedRaceGroupings, ParentAssetPack);

        foreach (var sg in model.Subgroups)
        {
            var subVm = _selfFactory(
                generalSettingsVM.RaceGroupings,
                Subgroups,
                ParentAssetPack,
                this,
                SetExplicitReferenceNPC);
            subVm.CopyInViewModelFromModel(sg, generalSettingsVM);
            Subgroups.Add(subVm);
        }

        //dds preview
        GetDDSPaths(ImagePaths);
    }
    public void RefreshListBoxLabel(ObservableCollection<VM_Subgroup> listSource, SubgroupListBox whichBox)
    {
        string label = "";
        if (listSource.Any())
        {
            label = "";
        }
        else
        {
            label = "Drag subgroups here from the tree view";
        }

        switch(whichBox)
        {
            case SubgroupListBox.Required: RequiredSubgroupsLabel = label; break;
            case SubgroupListBox.Excluded: ExcludedSubgroupsLabel = label; break;
        }
    }
    public enum SubgroupListBox
    {
        Required,
        Excluded
    }
    public void GetDDSPaths(ObservableCollection<ImagePreviewHandler.ImagePathWithSource> paths)
    {
        var ddsPaths = PathsMenu.Paths.Where(x => x.Source.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(System.IO.Path.Combine(PatcherEnvironmentProvider.Instance.Environment.DataFolderPath, x.Source)))
            .Select(x => x.Source)
            .Select(x => System.IO.Path.Combine(PatcherEnvironmentProvider.Instance.Environment.DataFolderPath, x))
            .ToHashSet();
        foreach (var path in ddsPaths)
        {
            var imagePathWithSource = new ImagePreviewHandler.ImagePathWithSource(path, this);
            if (!paths.Contains(imagePathWithSource))
            {
                paths.Add(imagePathWithSource);
            }
        }
        //paths.UnionWith(ddsPaths.Select(x => System.IO.Path.Combine(PatcherEnvironmentProvider.Instance.Environment.DataFolderPath, x)));
        foreach (var subgroup in Subgroups)
        {
            subgroup.GetDDSPaths(paths);
        }
    }

    public void CallRefreshTrackedBodyShapeDescriptorsC(object sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshTrackedBodyShapeDescriptors();
    }

    public void CallRefreshTrackedBodyShapeDescriptorsP(object sender, PropertyChangedEventArgs e)
    {
        RefreshTrackedBodyShapeDescriptors();
    }

    public void RefreshTrackedBodyShapeDescriptors()
    {
        if (this.ParentAssetPack.TrackedBodyGenConfig != null)
        {
            this.AllowedBodyGenDescriptors = new VM_BodyShapeDescriptorSelectionMenu(this.ParentAssetPack.TrackedBodyGenConfig.DescriptorUI, SubscribedRaceGroupings, ParentAssetPack);
            this.DisallowedBodyGenDescriptors = new VM_BodyShapeDescriptorSelectionMenu(this.ParentAssetPack.TrackedBodyGenConfig.DescriptorUI, SubscribedRaceGroupings, ParentAssetPack);
        }
    }

    public void AutoGenerateID_Children()
    {
        foreach (var subgroup in Subgroups)
        {
            subgroup.AutoGenerateID();
            subgroup.AutoGenerateID_Children();
        }
    }

    public void AutoGenerateID()
    {
        List<string> ids  = new();
        List<VM_Subgroup> parents = new();
        GetParents(parents);

        parents.Reverse();
        for (int i = 0; i < parents.Count; i++)
        {
            var parent = parents[i];
            if (parent.ID.IsNullOrWhitespace())
            {
                ids.Add("New");
            }
            else
            {
                var splitID = parent.ID.Split('.');
                ids.Add(splitID.Last());
            }
        }

        //get abbreviate for current subgroup name
        if (Name.IsNullOrWhitespace())
        {
            ids.Add("New");
        }
        else
        {
            var words = System.Text.RegularExpressions.Regex.Split(Name, @"\s+").Where(s => s != string.Empty).ToList();
            if (words.Count == 1 && words.First().Length <= 3)
            {
                ids.Add(Name);
            }
            else
            {
                var chars = new List<string>();
                foreach (var word in words)
                {
                    if (CanSplitByLettersAndNumbers(word, out var letterAndNumbers))
                    {
                        chars.Add(letterAndNumbers);
                    }
                    else
                    {
                        var candidate = Regex.Replace(word, "[^a-zA-Z0-9]", String.Empty); // remove non-alphanumeric
                        if (!candidate.IsNullOrEmpty())
                        {
                            if (candidate.IsNumeric())
                            {
                                chars.Add(candidate);
                            }
                            else
                            {
                                chars.Add(candidate.First().ToString());
                            }
                        }
                    }
                }
                ids.Add(string.Join("", chars));
            }
        }
       

        ID = string.Join('.', ids);
        ID = TrimTrailingNonAlphaNumeric(ID);

        EnumerateID();
    }

    public static string TrimTrailingNonAlphaNumeric(string s)
    {
        while (s != string.Empty && !char.IsLetterOrDigit(s.Last()))
        {
            s = s.Substring(0, s.Length - 1);
        }
        return s;
    }

    public void EnumerateID()
    {
        var newID = ID;
        ID = string.Empty;
        bool isUniqueID = false;
        HashSet<string> previousSplitNames = new();
        while (!isUniqueID)
        {
            if (ParentAssetPack.ContainsSubgroupID(newID))
            {
                var lastID = newID.Split('.').Last();
                if (lastID == null)
                {
                    newID = "New"; // don't think this should ever happen...
                }
                else if (CanSplitByLettersAndNumbers(newID, out string renamed1))
                {
                    newID = newID.Replace(lastID, renamed1);
                }
                else if (CanExtendWordSplit(lastID, Name, previousSplitNames, ParentAssetPack, out string renamed2))
                {
                    newID = newID.Replace(lastID, renamed2);
                }
                else if (lastID.Length < Name.Length)
                {
                    newID = newID.Replace(lastID, Name.Substring(0, lastID.Length + 1));
                }
                else
                {
                    newID = IncrementID(newID);
                }
            }
            else
            {
                ID = newID;
                isUniqueID = true;
            }
        }
    }

    public static bool CanExtendWordSplit(string s, string name, HashSet<string> previousNames, VM_AssetPack assetPack, out string renamed)
    {
        renamed = s;

        var split = name.Split();

        if (split.Length == 1)
        {
            return false;
        }

        string prefix = "";
        for (int i = 0; i < split.Length - 1; i++)
        {
            var candidate = Regex.Replace(split[i], "[^a-zA-Z0-9]", String.Empty);
            if (string.IsNullOrEmpty(candidate))
            {
                continue;
            }
            else
            {
                prefix += candidate.First();
            }
        }

        string lastWord = Regex.Replace(split.Last(), "[^a-zA-Z0-9]", String.Empty);
        string suffix = lastWord.First().ToString();

        string trial = prefix + suffix;

        while (suffix.Length <= lastWord.Length && assetPack.ContainsSubgroupID(trial))
        {
            suffix = lastWord.Substring(0, suffix.Length + 1);
            trial = prefix + suffix;
        }

        if (assetPack.ContainsSubgroupID(trial))
        {
            return false;
        }
        else
        {
            renamed = trial;
            return true;
        }
    }

    public bool CanSplitByLettersAndNumbers(string s, out string renamed)
    {
        if (s.IsNumeric()) { renamed = s; return false; }

        var trimNumbers = s.TrimEnd(" 1234567890".ToCharArray());
        if (trimNumbers == s)
        {
            renamed = s;
            return false;
        }

        var numbers = s.Substring(trimNumbers.Length, s.Length - trimNumbers.Length);
        var letters = trimNumbers.First().ToString();
        renamed = trimNumbers.First() + numbers;

        if (renamed == s)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public bool ContainsID(string id)
    {
        if (ID == id) { return true; }
        foreach (var subgroup in Subgroups)
        {
            if (subgroup.ContainsID(id))
            {
                return true;
            }
        }
        return false;
    }

    public static string IncrementID(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return "New";
        }
        if (id.Length < 2)
        {
            return id + "_1";
        }
        if (id.Contains('_'))
        {
            var split = id.Split('_');
            if (int.TryParse(split.Last(), out int index))
            {
                split[split.Length - 1] = (index + 1).ToString();
                return String.Join('_', split);
            }
            else
            {
                return id + "_1";
            }
        }
        return id + "_1";
    }

    public void GetParents(List<VM_Subgroup> parents)
    {
        if (ParentSubgroup is not null)
        {
            parents.Add(ParentSubgroup);
            ParentSubgroup.GetParents(parents);
        }
    }

    public static AssetPack.Subgroup DumpViewModelToModel(VM_Subgroup viewModel)
    {
        var model = new AssetPack.Subgroup();

        model.ID = viewModel.ID;
        model.Name = viewModel.Name;
        model.Enabled = viewModel.Enabled;
        model.DistributionEnabled = viewModel.DistributionEnabled;
        model.AllowedRaces = viewModel.AllowedRaces.ToHashSet();
        model.AllowedRaceGroupings = viewModel.AllowedRaceGroupings.RaceGroupingSelections.Where(x => x.IsSelected).Select(x => x.SubscribedMasterRaceGrouping.Label).ToHashSet();
        model.DisallowedRaces = viewModel.DisallowedRaces.ToHashSet();
        model.DisallowedRaceGroupings = viewModel.DisallowedRaceGroupings.RaceGroupingSelections.Where(x => x.IsSelected).Select(x => x.SubscribedMasterRaceGrouping.Label).ToHashSet();
        model.AllowedAttributes = VM_NPCAttribute.DumpViewModelsToModels(viewModel.AllowedAttributes);
        model.DisallowedAttributes = VM_NPCAttribute.DumpViewModelsToModels(viewModel.DisallowedAttributes);
        model.AllowUnique = viewModel.AllowUnique;
        model.AllowNonUnique = viewModel.AllowNonUnique;
        model.RequiredSubgroups = viewModel.RequiredSubgroups.Select(x => x.ID).ToHashSet();
        model.ExcludedSubgroups = viewModel.ExcludedSubgroups.Select(x => x.ID).ToHashSet();
        model.AddKeywords = viewModel.AddKeywords.Select(x => x.Content).ToHashSet();
        model.ProbabilityWeighting = viewModel.ProbabilityWeighting;
        model.Paths = VM_FilePathReplacementMenu.DumpViewModelToModels(viewModel.PathsMenu);
        model.WeightRange = viewModel.WeightRange;

        model.AllowedBodyGenDescriptors = VM_BodyShapeDescriptorSelectionMenu.DumpToHashSet(viewModel.AllowedBodyGenDescriptors);
        model.DisallowedBodyGenDescriptors = VM_BodyShapeDescriptorSelectionMenu.DumpToHashSet(viewModel.DisallowedBodyGenDescriptors);
        model.AllowedBodySlideDescriptors = VM_BodyShapeDescriptorSelectionMenu.DumpToHashSet(viewModel.AllowedBodySlideDescriptors);
        model.DisallowedBodySlideDescriptors = VM_BodyShapeDescriptorSelectionMenu.DumpToHashSet(viewModel.DisallowedBodySlideDescriptors);

        foreach (var sg in viewModel.Subgroups)
        {
            model.Subgroups.Add(DumpViewModelToModel(sg));
        }

        return model;
    }

    public object Clone()
    {
        return this.Clone(this.ParentCollection);
    }

    public object Clone(ObservableCollection<VM_Subgroup> parentCollection)
    {
        var clone = _selfFactory(this.SubscribedRaceGroupings, parentCollection, this.ParentAssetPack, this, this.SetExplicitReferenceNPC);
        clone.AddKeywords = new ObservableCollection<VM_CollectionMemberString>(this.AddKeywords);
        clone.AllowedAttributes = new ObservableCollection<VM_NPCAttribute>(this.AllowedAttributes);
        clone.DisallowedAttributes = new ObservableCollection<VM_NPCAttribute>(this.DisallowedAttributes);
        if (this.AllowedBodyGenDescriptors is not null)
        {
            clone.AllowedBodyGenDescriptors = this.AllowedBodyGenDescriptors.Clone();
        }
        if (this.DisallowedBodyGenDescriptors is not null)
        {
            clone.DisallowedBodyGenDescriptors = this.DisallowedBodyGenDescriptors.Clone();
        }
        clone.AllowedBodySlideDescriptors = this.AllowedBodySlideDescriptors.Clone();
        clone.DisallowedBodySlideDescriptors = this.DisallowedBodySlideDescriptors.Clone();
        clone.AllowedRaceGroupings = this.AllowedRaceGroupings.Clone();
        clone.DisallowedRaceGroupings = this.DisallowedRaceGroupings.Clone();
        clone.AllowedRaces = new ObservableCollection<FormKey>(this.AllowedRaces);
        clone.DisallowedRaces = new ObservableCollection<FormKey>(this.DisallowedRaces);
        clone.AllowUnique = this.AllowUnique;
        clone.AllowNonUnique = this.AllowNonUnique;
        clone.DistributionEnabled = this.DistributionEnabled;
        clone.ID = this.ID;
        clone.Name = this.Name;
        clone.RequiredSubgroupIDs = new HashSet<string>(RequiredSubgroupIDs);
        clone.ExcludedSubgroupIDs = new HashSet<string>(ExcludedSubgroupIDs);
        clone.RequiredSubgroups = new ObservableCollection<VM_Subgroup>(this.RequiredSubgroups);
        clone.ExcludedSubgroups = new ObservableCollection<VM_Subgroup>(this.ExcludedSubgroups);
        clone.WeightRange = this.WeightRange.Clone();
        clone.ProbabilityWeighting = this.ProbabilityWeighting;
        clone.PathsMenu = this.PathsMenu.Clone();
        clone.GetDDSPaths(clone.ImagePaths);

        clone.Subgroups.Clear();
        foreach (var subgroup in this.Subgroups)
        {
            clone.Subgroups.Add(subgroup.Clone(clone.Subgroups) as VM_Subgroup);
        }

        return clone;
    }

    public void DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is VM_Subgroup)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            dropInfo.Effects = DragDropEffects.Move;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is VM_Subgroup && dropInfo.VisualTarget is ListBox)
        {
            var listBox = (ListBox)dropInfo.VisualTarget;
            var parentContextSubgroup = listBox.DataContext as VM_Subgroup;
            if (parentContextSubgroup != null)
            {
                var draggedSubgroup = (VM_Subgroup)dropInfo.Data;
                if (listBox.Name == "lbRequiredSubgroups")
                {
                    parentContextSubgroup.RequiredSubgroups.Add(draggedSubgroup);
                    parentContextSubgroup.RequiredSubgroupIDs.Add(draggedSubgroup.ID);
                    RefreshListBoxLabel(parentContextSubgroup.RequiredSubgroups, SubgroupListBox.Required);
                }
                else if (listBox.Name == "lbExcludedSubgroups")
                {
                    parentContextSubgroup.ExcludedSubgroups.Add(draggedSubgroup);
                    parentContextSubgroup.ExcludedSubgroupIDs.Add(draggedSubgroup.ID);
                    RefreshListBoxLabel(parentContextSubgroup.ExcludedSubgroups, SubgroupListBox.Excluded);
                }
            }
        }
    }

    public bool IsParentOf(VM_Subgroup candidateChild)
    {
        foreach (var subgroup in Subgroups)
        {
            if (candidateChild == subgroup)
            { return true; }
            else if (subgroup.IsParentOf(candidateChild))
            {
                return true;
            }
        }
        return false;
    }


}