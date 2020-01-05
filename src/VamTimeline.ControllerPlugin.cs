using System;
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
    public class ControllerPlugin : MVRScript
    {
        private const string AllTargets = "(All)";
        private Atom _atom;
        private SimpleSignUI _ui;
        private JSONStorableStringChooser _atomsJSON;
        private JSONStorableStringChooser _animationJSON;
        private JSONStorableFloat _scrubberJSON;
        private JSONStorableAction _playJSON;
        private JSONStorableAction _playIfNotPlayingJSON;
        private JSONStorableAction _stopJSON;
        private JSONStorableStringChooser _atomsToLink;
        private LinkedAnimation _mainLinkedAnimation;
        private JSONStorableString _savedAtomsJSON;
        private JSONStorableStringChooser _targetJSON;
        private JSONStorableString _displayJSON;
        private UIDynamicButton _linkButton;
        private readonly List<LinkedAnimation> _linkedAnimations = new List<LinkedAnimation>();

        #region Initialization

        public override void Init()
        {
            try
            {
                _atom = GetAtom();
                InitStorables();
                InitCustomUI();
                OnEnable();
            }
            catch (Exception exc)
            {
                SuperController.LogError("VamTimeline.ControllerPlugin.Init: " + exc);
            }
        }

        private Atom GetAtom()
        {
            // Note: Yeah, that's horrible, but containingAtom is null
            var container = gameObject?.transform?.parent?.parent?.parent?.parent?.parent?.gameObject;
            if (container == null)
                throw new NullReferenceException($"Could not find the parent gameObject");
            var atom = container.GetComponent<Atom>();
            if (atom == null)
                throw new NullReferenceException($"Could not find the parent atom in {container.name}");
            if (atom.type != "SimpleSign")
                throw new InvalidOperationException("Can only be applied on SimpleSign");
            return atom;
        }

        private void InitStorables()
        {
            _atomsJSON = new JSONStorableStringChooser("Atoms", new List<string> { "" }, "", "Atoms", (string v) => SelectCurrentAtom(v))
            {
                isStorable = false
            };
            RegisterStringChooser(_atomsJSON);

            _animationJSON = new JSONStorableStringChooser("Animation", new List<string>(), "", "Animation", (string v) => ChangeAnimation(v))
            {
                isStorable = false
            };
            RegisterStringChooser(_animationJSON);

            _playJSON = new JSONStorableAction("Play", () => Play());
            RegisterAction(_playJSON);

            _playIfNotPlayingJSON = new JSONStorableAction("Play If Not Playing", () => PlayIfNotPlaying());
            RegisterAction(_playIfNotPlayingJSON);

            _stopJSON = new JSONStorableAction("Stop", () => Stop());
            RegisterAction(_stopJSON);

            _targetJSON = new JSONStorableStringChooser(StorableNames.FilterAnimationTarget, new List<string>(), AllTargets, StorableNames.FilterAnimationTarget, (string v) => SelectTargetFilter(v))
            {
                isStorable = false
            };
            RegisterStringChooser(_targetJSON);

            _scrubberJSON = new JSONStorableFloat("Time", 0f, v => ChangeTime(v), 0f, 5f, true)
            {
                isStorable = false
            };
            RegisterFloat(_scrubberJSON);

            _displayJSON = new JSONStorableString("Display", "")
            {
                isStorable = false
            };
            RegisterString(_displayJSON);

            var atoms = GetAtomsWithVamTimeline().ToList();
            _atomsToLink = new JSONStorableStringChooser("Atom To Link", atoms, atoms.FirstOrDefault() ?? "", "Add");

            _savedAtomsJSON = new JSONStorableString("Atoms", "", (string v) => StartCoroutine(RestoreAtomsLink(v)));
            RegisterString(_savedAtomsJSON);
        }

        private IEnumerable<string> GetAtomsWithVamTimeline()
        {
            var atoms = SuperController.singleton.GetAtoms();
            foreach (var atom in atoms)
            {
                if (atom.GetStorableIDs().Any(id => id.EndsWith("VamTimeline.AtomPlugin") || id.EndsWith("VamTimeline.FloatParamsPlugin")))
                {
                    if (_linkedAnimations.Any(la => la.Atom.uid == atom.uid)) continue;

                    yield return atom.uid;
                }
            }
        }

        private IEnumerator RestoreAtomsLink(string savedAtoms)
        {
            // This is an ugly way to wait for the target atom restore
            yield return new WaitForEndOfFrame();

            if (!string.IsNullOrEmpty(savedAtoms))
            {
                foreach (var atomUid in savedAtoms.Split(';'))
                {
                    LinkAtom(atomUid);
                }
            }
        }

        private void InitCustomUI()
        {
            var atomsToLinkUI = CreateScrollablePopup(_atomsToLink);
            atomsToLinkUI.popupPanelHeight = 800f;
            atomsToLinkUI.popup.onOpenPopupHandlers += () => _atomsToLink.choices = GetAtomsWithVamTimeline().ToList();

            _linkButton = CreateButton("Link");
            _linkButton.button.interactable = _atomsToLink.choices.Count > 0;
            _linkButton.button.onClick.AddListener(() => LinkAtom(_atomsToLink.val));

            var resyncButton = CreateButton("Re-Sync Atom Plugins");
            resyncButton.button.onClick.AddListener(() => RestoreAtomsLink(_savedAtomsJSON.val));
        }

        #endregion

        #region Lifecycle

        public void OnEnable()
        {
            if (_atom == null) return;

            try
            {
                var x = 0f;
                var y = -0.39f;
                // const float baseWidth = 1160f;
                _ui = new SimpleSignUI(_atom, this);
                _ui.CreateUIPopupInCanvas(_animationJSON, x, y + 0.355f);
                _ui.CreateUISliderInCanvas(_scrubberJSON, x, y + 0.14f);
                _ui.CreateUIButtonInCanvas("\u25B6 Play", x - 0.105f, y + 0.60f, 810f, 100f).button.onClick.AddListener(() => Play());
                _ui.CreateUIButtonInCanvas("\u25A0 Stop", x + 0.257f, y + 0.60f, 300f, 100f).button.onClick.AddListener(() => Stop());
                _ui.CreateUIPopupInCanvas(_atomsJSON, x, y + 0.585f);
                _ui.CreateUIPopupInCanvas(_targetJSON, x, y + 0.655f);
                _ui.CreateUIButtonInCanvas("\u2190 Previous Frame", x - 0.182f, y + 0.82f, 550f, 100f).button.onClick.AddListener(() => PreviousFrame());
                _ui.CreateUIButtonInCanvas("Next Frame \u2192", x + 0.182f, y + 0.82f, 550f, 100f).button.onClick.AddListener(() => NextFrame());
                _ui.CreateUITextfieldInCanvas(_displayJSON, x, y + 0.62f);
            }
            catch (Exception exc)
            {
                SuperController.LogError("VamTimeline.ControllerPlugin.OnEnable: " + exc);
            }
        }

        public void OnDisable()
        {
            if (_ui == null) return;

            try
            {
                _scrubberJSON.slider = null;
                _ui.Dispose();
                _ui = null;
            }
            catch (Exception exc)
            {
                SuperController.LogError("VamTimeline.ControllerPlugin.OnDisable: " + exc);
            }
        }

        public void OnDestroy()
        {
            OnDisable();
        }

        #endregion

        private void ChangeTime(float v)
        {
            {
                if (_mainLinkedAnimation == null) return;
                foreach (var link in _linkedAnimations)
                    link.Scrubber.val = v;
                VamTimelineContextChanged(_mainLinkedAnimation.Atom.uid);
            }
        }

        private void LinkAtom(string uid)
        {
            try
            {
                if (_linkedAnimations.Any(la => la.Atom.uid == uid)) return;

                var atom = SuperController.singleton.GetAtomByUid(uid);
                if (atom == null) return;
                LinkAnimationPlugin(atom, "VamTimeline.AtomPlugin", "Controllers");
                LinkAnimationPlugin(atom, "VamTimeline.FloatParamsPlugin", "Params");
            }
            catch (Exception exc)
            {
                SuperController.LogError("VamTimeline.ControllerPlugin.LinkAtom: " + exc);
            }
        }

        private void LinkAnimationPlugin(Atom atom, string pluginNameSuffix, string labelSuffix)
        {
            var link = LinkedAnimation.TryCreate(atom, pluginNameSuffix, labelSuffix);
            if (link == null) return;
            _linkedAnimations.Add(link);
            _atomsJSON.choices = _linkedAnimations.Select(la => la.Label).ToList();
            if (_mainLinkedAnimation == null)
                SelectCurrentAtom(link.Label);
            // TODO: If an atom contains ';' it won't work
            _savedAtomsJSON.val = string.Join(";", _linkedAnimations.Select(la => la.Atom.uid).Distinct().ToArray());
            _atomsToLink.choices = GetAtomsWithVamTimeline().ToList();
            _atomsToLink.val = _atomsToLink.choices.FirstOrDefault() ?? "";
        }

        public void VamTimelineAnimationUpdated(string uid)
        {
            VamTimelineContextChanged(uid);

            if (_mainLinkedAnimation == null || _mainLinkedAnimation.Atom.uid != uid)
                return;

            _scrubberJSON.slider.interactable = true;
            _scrubberJSON.valNoCallback = _mainLinkedAnimation.Scrubber.val;
            _animationJSON.choices = _mainLinkedAnimation.Animation.choices;
            _targetJSON.choices = _mainLinkedAnimation.FilterAnimationTarget.choices;
        }

        public void VamTimelineContextChanged(string uid)
        {
            if (_linkedAnimations.Count == 0) return;

            var firstAnimationName = _linkedAnimations[0].Animation.val;
            if (_linkedAnimations.Skip(1).All(la => la.Animation.val == firstAnimationName))
                _animationJSON.valNoCallback = firstAnimationName;
            else
                _animationJSON.valNoCallback = "(Multiple animations selected)";

            if (_mainLinkedAnimation == null || _mainLinkedAnimation.Atom.uid != uid)
                return;

            _scrubberJSON.max = _mainLinkedAnimation.Scrubber.max;
            var target = _mainLinkedAnimation.FilterAnimationTarget.val;
            _targetJSON.valNoCallback = string.IsNullOrEmpty(target) ? AllTargets : target;
            _displayJSON.valNoCallback = _mainLinkedAnimation.Display.val;
        }

        public void Update()
        {
            try
            {
                if (_mainLinkedAnimation == null) return;

                var scrubber = _mainLinkedAnimation.Scrubber;
                if (scrubber != null && scrubber.val != _scrubberJSON.val)
                    _scrubberJSON.valNoCallback = scrubber.val;
            }
            catch (Exception exc)
            {
                SuperController.LogError("VamTimeline.ControllerPlugin.Update: " + exc);
            }
        }

        private void SelectCurrentAtom(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                _mainLinkedAnimation = null;
                return;
            }
            _mainLinkedAnimation = _linkedAnimations.FirstOrDefault(la => la.Label == label);
            if (_mainLinkedAnimation == null) return;
            _atomsJSON.valNoCallback = _mainLinkedAnimation.Label;
            VamTimelineAnimationUpdated(_mainLinkedAnimation.Atom.uid);
            VamTimelineContextChanged(_mainLinkedAnimation.Atom.uid);
        }

        private void ChangeAnimation(string name)
        {
            foreach (var animation in _linkedAnimations)
            {
                animation.ChangeAnimation(name);
            }
        }

        private void Play()
        {
            foreach (var animation in _linkedAnimations)
            {
                animation.Play();
            }
        }

        private void PlayIfNotPlaying()
        {
            foreach (var animation in _linkedAnimations)
            {
                animation.PlayIfNotPlaying();
            }
        }

        private void Stop()
        {
            foreach (var animation in _linkedAnimations)
            {
                animation.Stop();
            }
        }

        private void NextFrame()
        {
            if (_mainLinkedAnimation == null) return;
            _mainLinkedAnimation.NextFrame();
            var time = _mainLinkedAnimation.Scrubber.val;
            foreach (var animation in _linkedAnimations.Where(la => la != _mainLinkedAnimation))
            {
                animation.Scrubber.val = time;
            }
        }

        private void PreviousFrame()
        {
            if (_mainLinkedAnimation == null) return;
            _mainLinkedAnimation.PreviousFrame();
            var time = _mainLinkedAnimation.Scrubber.val;
            foreach (var animation in _linkedAnimations.Where(la => la != _mainLinkedAnimation))
            {
                animation.Scrubber.val = time;
            }
        }

        private void SelectTargetFilter(string v)
        {
            if (_mainLinkedAnimation == null) return;
            if (string.IsNullOrEmpty(v) || v == AllTargets)
            {
                _mainLinkedAnimation.FilterAnimationTarget.val = null;
                return;
            }
            _mainLinkedAnimation.FilterAnimationTarget.val = v;
        }
    }
}