using System;
using System.Collections.Generic;
using System.Linq;
using MVR.FileManagementSecure;
using SimpleJSON;

namespace VamTimeline
{
    /// <summary>
    /// VaM Timeline
    /// By Acidbubbles
    /// Animation timeline with keyframes
    /// Source: https://github.com/acidbubbles/vam-timeline
    /// </summary>
    public class MoreScreen : ScreenBase
    {
        private const string _saveExt = "json";
        private const string _saveFolder = "Saves\\animations";

        public const string ScreenName = "More...";
        public override string Name => ScreenName;

        private JSONStorableStringChooser _exportAnimationsJSON;

        public MoreScreen(IAtomPlugin plugin)
            : base(plugin)
        {

        }
        public override void Init()
        {
            base.Init();

            // Right side

            InitSpeedUI(true);

            CreateSpacer(true);

            CreateChangeScreenButton("Edit Animation...", EditAnimationScreen.ScreenName, true);
            CreateChangeScreenButton("Create / Manage Animations...", ManageAnimationsScreen.ScreenName, true);
            CreateChangeScreenButton("Settings...", SettingsScreen.ScreenName, true);
            CreateChangeScreenButton("Advanced...", AdvancedScreen.ScreenName, true);

            CreateSpacer(true);

            CreateChangeScreenButton("Help", HelpScreen.ScreenName, true);

            CreateSpacer(true);

            InitImportExportUI(true);
        }

        private void InitImportExportUI(bool rightSide)
        {
            _exportAnimationsJSON = new JSONStorableStringChooser("Export Animation", new List<string> { "(All)" }.Concat(Plugin.Animation.GetAnimationNames()).ToList(), "(All)", "Export Animation")
            {
                isStorable = false
            };
            RegisterStorable(_exportAnimationsJSON);
            var exportAnimationsUI = Plugin.CreateScrollablePopup(_exportAnimationsJSON, rightSide);
            RegisterComponent(exportAnimationsUI);

            var exportUI = Plugin.CreateButton("Export animation", rightSide);
            exportUI.button.onClick.AddListener(() => Export());
            RegisterComponent(exportUI);

            var importUI = Plugin.CreateButton("Import animation", rightSide);
            importUI.button.onClick.AddListener(() => Import());
            RegisterComponent(importUI);
        }

        private void InitSpeedUI(bool rightSide)
        {
            RegisterStorable(Plugin.SpeedJSON);
            var speedUI = Plugin.CreateSlider(Plugin.SpeedJSON, rightSide);
            speedUI.valueFormat = "F3";
            RegisterComponent(speedUI);
        }

        private void Export()
        {
            try
            {
                FileManagerSecure.CreateDirectory(_saveFolder);
                var fileBrowserUI = SuperController.singleton.fileBrowserUI;
                fileBrowserUI.SetTitle("Save animation");
                fileBrowserUI.fileRemovePrefix = null;
                fileBrowserUI.hideExtension = false;
                fileBrowserUI.keepOpen = false;
                fileBrowserUI.fileFormat = _saveExt;
                fileBrowserUI.defaultPath = _saveFolder;
                fileBrowserUI.showDirs = true;
                fileBrowserUI.shortCuts = null;
                fileBrowserUI.browseVarFilesAsDirectories = false;
                fileBrowserUI.SetTextEntry(true);
                fileBrowserUI.Show(ExportFileSelected);
                fileBrowserUI.ActivateFileNameField();
            }
            catch (Exception exc)
            {
                SuperController.LogError($"VamTimeline: Failed to save file dialog: {exc}");
            }
        }

        private void ExportFileSelected(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!path.ToLower().EndsWith($".{_saveExt}")) path += $".{_saveExt}";

            try
            {
                var jc = Plugin.GetAnimationJSON(_exportAnimationsJSON.val == "(All)" ? null : _exportAnimationsJSON.val);
                jc["AtomType"] = Plugin.ContainingAtom.type;
                var atomState = new JSONClass();
                var allTargets = new HashSet<FreeControllerV3>(
                    Plugin.Animation.Clips
                        .Where(c => _exportAnimationsJSON.val == "(All)" || c.AnimationName == _exportAnimationsJSON.val)
                        .SelectMany(c => c.TargetControllers)
                        .Select(t => t.Controller)
                        .Distinct());
                foreach (var fc in Plugin.ContainingAtom.freeControllers)
                {
                    if (fc.name == "control") continue;
                    if (!fc.name.EndsWith("Control")) continue;
                    atomState[fc.name] = new JSONClass
                    {
                        {"currentPositionState", ((int)fc.currentPositionState).ToString()},
                        {"localPosition", AtomAnimationSerializer.SerializeVector3(fc.transform.localPosition)},
                        {"currentRotationState", ((int)fc.currentRotationState).ToString()},
                        {"localRotation", AtomAnimationSerializer.SerializeQuaternion(fc.transform.localRotation)}
                    };
                }
                jc["ControllersState"] = atomState;
                SuperController.singleton.SaveJSON(jc, path);
                SuperController.singleton.DoSaveScreenshot(path);
            }
            catch (Exception exc)
            {
                SuperController.LogError($"VamTimeline: Failed to export animation: {exc}");
            }
        }

        private void Import()
        {
            try
            {
                FileManagerSecure.CreateDirectory(_saveFolder);
                var shortcuts = FileManagerSecure.GetShortCutsForDirectory(_saveFolder);
                SuperController.singleton.GetMediaPathDialog(ImportFileSelected, _saveExt, _saveFolder, false, true, false, null, false, shortcuts);
            }
            catch (Exception exc)
            {
                SuperController.LogError($"VamTimeline: Failed to open file dialog: {exc}");
            }
        }

        private void ImportFileSelected(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var json = SuperController.singleton.LoadJSON(path);
                if (json["AtomType"]?.Value != Plugin.ContainingAtom.type)
                {
                    SuperController.LogError($"VamTimeline: Loaded animation for {json["AtomType"]} but current atom type is {Plugin.ContainingAtom.type}");
                    return;
                }

                var jc = json.AsObject;
                if (jc.HasKey("ControllersState"))
                {
                    var controllersState = jc["ControllersState"].AsObject;
                    foreach (var k in controllersState.Keys)
                    {
                        var fc = Plugin.ContainingAtom.freeControllers.FirstOrDefault(x => x.name == k);
                        if (fc == null)
                        {
                            SuperController.LogError($"VamTimeline: Loaded animation had state for controller {k} but no such controller were found on this atom.");
                            continue;
                        }
                        var state = controllersState[k];
                        fc.currentPositionState = (FreeControllerV3.PositionState)state["currentPositionState"].AsInt;
                        fc.transform.localPosition = AtomAnimationSerializer.DeserializeVector3(state["localPosition"].AsObject);
                        fc.currentRotationState = (FreeControllerV3.RotationState)state["currentRotationState"].AsInt;
                        fc.transform.localRotation = AtomAnimationSerializer.DeserializeQuaternion(state["localRotation"].AsObject);
                    }
                }

                Plugin.Load(jc);
                Plugin.ChangeAnimation(jc["Clips"][0]["AnimationName"].Value);
                Plugin.Animation.Stop();
            }
            catch (Exception exc)
            {
                SuperController.LogError($"VamTimeline.{nameof(AdvancedScreen)}.{nameof(ImportFileSelected)}: Failed to import animation: {exc}");
            }
        }
    }
}
