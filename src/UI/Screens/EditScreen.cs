using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VamTimeline
{
    /// <summary>
    /// VaM Timeline
    /// By Acidbubbles
    /// Animation timeline with keyframes
    /// Source: https://github.com/acidbubbles/vam-timeline
    /// </summary>
    public class EditScreen : ScreenBase
    {
        public const string ScreenName = "Edit";

        public override string screenId => ScreenName;

        private const string _noKeyframeCurveType = "(No Keyframe)";
        private const string _loopCurveType = "(Loop)";

        private class TargetRef
        {
            public ITargetFrame Component;
            public IAnimationTargetWithCurves Target;
        }

        private readonly List<TargetRef> _targets = new List<TargetRef>();
        private JSONStorableStringChooser _curveTypeJSON;
        private Curves _curves;
        private UIDynamicPopup _curveTypeUI;
        private bool _selectionChangedPending;
        private UIDynamicButton _manageTargetsUI;

        public EditScreen()
            : base()
        {

        }
        public override void Init(IAtomPlugin plugin)
        {
            base.Init(plugin);

            // Left side

            InitChangeCurveTypeUI(false);

            InitCurvesUI(false);

            InitClipboardUI(false);

            InitAutoKeyframeUI();

            // Right side

            current.onTargetsSelectionChanged.AddListener(OnSelectionChanged);
            animation.onTimeChanged.AddListener(OnTimeChanged);

            OnSelectionChanged();

            if (animation.IsEmpty()) InitExplanation();
        }

        private void InitChangeCurveTypeUI(bool rightSide)
        {
            _curveTypeJSON = new JSONStorableStringChooser(StorableNames.ChangeCurve, CurveTypeValues.DisplayCurveTypes, "", "Change Curve", ChangeCurve);
            RegisterStorable(_curveTypeJSON);
            _curveTypeUI = CreateScrollablePopup(_curveTypeJSON, rightSide);
            _curveTypeUI.popupPanelHeight = 450f;
            RegisterComponent(_curveTypeUI);
        }

        private void InitAutoKeyframeUI()
        {
            RegisterStorable(plugin.autoKeyframeAllControllersJSON);
            var autoKeyframeAllControllersUI = CreateToggle(plugin.autoKeyframeAllControllersJSON, false);
            RegisterComponent(autoKeyframeAllControllersUI);
        }

        private void InitCurvesUI(bool rightSide)
        {
            var spacerUI = CreateSpacer(rightSide);
            spacerUI.height = 300f;
            RegisterComponent(spacerUI);

            _curves = spacerUI.gameObject.AddComponent<Curves>();
        }

        private void InitExplanation()
        {
            var textJSON = new JSONStorableString("Help", HelpScreen.HelpText);
            RegisterStorable(textJSON);
            var textUI = CreateTextField(textJSON, true);
            textUI.height = 900;
            RegisterComponent(textUI);
        }

        private void RefreshCurves()
        {
            if (_curves == null) return;
            _curves.Bind(animation, current.allTargetsCount == 1 ? current.allTargets.ToList() : current.GetSelectedTargets().ToList());
        }

        protected override void OnCurrentAnimationChanged(AtomAnimation.CurrentAnimationChangedEventArgs args)
        {
            base.OnCurrentAnimationChanged(args);
            args.before.onTargetsSelectionChanged.RemoveListener(OnSelectionChanged);
            args.after.onTargetsSelectionChanged.AddListener(OnSelectionChanged);
            RefreshTargetsList();
        }

        private void OnTimeChanged(AtomAnimation.TimeChangedEventArgs args)
        {
            RefreshCurrentCurveType(args.currentClipTime);
        }

        private void OnSelectionChanged()
        {
            if (_selectionChangedPending) return;
            _selectionChangedPending = true;
            StartCoroutine(SelectionChangedDeferred());
        }

        private IEnumerator SelectionChangedDeferred()
        {
            yield return new WaitForEndOfFrame();
            _selectionChangedPending = false;
            if (_disposing) yield break;
            RefreshCurrentCurveType(animation.clipTime);
            RefreshCurves();
            RefreshTargetsList();
            _curveTypeUI.popup.topButton.interactable = current.GetAllOrSelectedTargets().OfType<FreeControllerAnimationTarget>().Count() > 0;
        }

        private void RefreshCurrentCurveType(float currentClipTime)
        {
            if (_curveTypeJSON == null) return;

            var time = currentClipTime.Snap();
            if (current.loop && (time.IsSameFrame(0) || time.IsSameFrame(current.animationLength)))
            {
                _curveTypeJSON.valNoCallback = _loopCurveType;
                return;
            }
            var ms = time.ToMilliseconds();
            var curveTypes = new HashSet<string>();
            foreach (var target in current.GetAllOrSelectedTargets().OfType<FreeControllerAnimationTarget>())
            {
                KeyframeSettings v;
                if (!target.settings.TryGetValue(ms, out v)) continue;
                curveTypes.Add(v.curveType);
            }
            if (curveTypes.Count == 0)
                _curveTypeJSON.valNoCallback = _noKeyframeCurveType;
            else if (curveTypes.Count == 1)
                _curveTypeJSON.valNoCallback = curveTypes.First().ToString();
            else
                _curveTypeJSON.valNoCallback = "(" + string.Join("/", curveTypes.ToArray()) + ")";
        }

        private void RefreshTargetsList()
        {
            if (animation == null) return;
            RemoveTargets();
            RemoveButton(_manageTargetsUI);
            var time = animation.clipTime;

            foreach (var target in current.GetAllOrSelectedTargets().OfType<FreeControllerAnimationTarget>())
            {
                var keyframeUI = CreateSpacer(true);
                keyframeUI.height = 60f;
                var component = keyframeUI.gameObject.AddComponent<ControllerTargetFrame>();
                component.Bind(plugin, animation.current, target);
                _targets.Add(new TargetRef
                {
                    Component = component,
                    Target = target
                });
            }

            foreach (var target in current.GetAllOrSelectedTargets().OfType<FloatParamAnimationTarget>())
            {
                var keyframeUI = CreateSpacer(true);
                keyframeUI.height = 60f;
                var component = keyframeUI.gameObject.AddComponent<FloatParamTargetFrame>();
                component.Bind(plugin, animation.current, target);
                _targets.Add(new TargetRef
                {
                    Component = component,
                    Target = target,
                });
            }
            _manageTargetsUI = CreateChangeScreenButton("<b>[+/-]</b> Add/Remove Targets", TargetsScreen.ScreenName, true);
            if (current.allTargetsCount == 0)
                _manageTargetsUI.buttonColor = new Color(0f, 1f, 0f);
            else
                _manageTargetsUI.buttonColor = new Color(0.8f, 0.7f, 0.8f);
        }

        public override void OnDestroy()
        {
            current.onTargetsSelectionChanged.RemoveListener(OnSelectionChanged);
            animation.onTimeChanged.RemoveListener(OnTimeChanged);
            RemoveButton(_manageTargetsUI);
            RemoveTargets();
            base.OnDestroy();
        }

        private void RemoveTargets()
        {
            foreach (var targetRef in _targets)
            {
                RemoveSpacer(targetRef.Component.Container);
            }
            _targets.Clear();
        }

        private void ChangeCurve(string curveType)
        {
            if (string.IsNullOrEmpty(curveType) || curveType.StartsWith("("))
            {
                RefreshCurrentCurveType(animation.clipTime);
                return;
            }
            float time = animation.clipTime.Snap();
            if (time.IsSameFrame(0) && curveType == CurveTypeValues.CopyPrevious)
            {
                RefreshCurrentCurveType(animation.clipTime);
                return;
            }
            if (animation.isPlaying) return;
            if (current.loop && (time.IsSameFrame(0) || time.IsSameFrame(current.animationLength)))
            {
                RefreshCurrentCurveType(animation.clipTime);
                return;
            }
            current.ChangeCurve(time, curveType);
            RefreshCurrentCurveType(animation.clipTime);
        }
    }
}
