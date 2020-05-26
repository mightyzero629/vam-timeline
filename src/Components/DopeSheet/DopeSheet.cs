using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VamTimeline
{
    /// <summary>
    /// VaM Timeline
    /// By Acidbubbles
    /// Animation timeline with keyframes
    /// Source: https://github.com/acidbubbles/vam-timeline
    /// </summary>
    public class DopeSheet : MonoBehaviour
    {
        private readonly DopeSheetStyle _style = new DopeSheetStyle();
        private readonly RectTransform _scrubberRect;
        public readonly ScrollRect _scrollRect;
        private readonly VerticalLayoutGroup _layout;
        private float _scrubberMax;
        private IAtomAnimationClip _clip;
        private List<DopeSheetKeyframes> _keyframesRows = new List<DopeSheetKeyframes>();

        public DopeSheet()
        {
            gameObject.AddComponent<Canvas>();
            gameObject.AddComponent<GraphicRaycaster>();

            CreateBackground(gameObject, _style.BackgroundColor);

            _scrubberRect = CreateScrubber(gameObject, _style.ScrubberColor).GetComponent<RectTransform>();

            var scrollView = CreateScrollView(gameObject);
            var viewport = CreateViewport(scrollView);
            var content = CreateContent(viewport);
            _scrollRect = scrollView.GetComponent<ScrollRect>();
            _scrollRect.viewport = viewport.GetComponent<RectTransform>();
            _scrollRect.content = content.GetComponent<RectTransform>();
            _layout = content.GetComponent<VerticalLayoutGroup>();
        }

        public void Start()
        {
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        public void OnDestroy()
        {
            Unbind();
        }

        private GameObject CreateBackground(GameObject parent, Color color)
        {
            var go = new GameObject();
            go.transform.SetParent(parent.transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.StretchParent();

            var image = go.AddComponent<Image>();
            image.color = color;

            return go;
        }

        private GameObject CreateScrubber(GameObject parent, Color color)
        {
            var go = new GameObject("Scrubber");
            go.transform.SetParent(parent.transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.StretchParent();
            rect.anchoredPosition = new Vector2(_style.LabelWidth / 2f, 0);
            rect.sizeDelta = new Vector2(-_style.LabelWidth - _style.KeyframesRowPadding * 2f, 0);

            var line = new GameObject("Scrubber Line");
            line.transform.SetParent(go.transform, false);

            var lineRect = line.AddComponent<RectTransform>();
            lineRect.StretchCenter();
            lineRect.sizeDelta = new Vector2(_style.ScrubberSize, 0);

            var image = line.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return line;
        }

        private GameObject CreateScrollView(GameObject parent)
        {
            var go = new GameObject("Scroll View");
            go.transform.SetParent(parent.transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.StretchParent();

            var scroll = go.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            return go;
        }

        private GameObject CreateViewport(GameObject scrollView)
        {
            var go = new GameObject("Viewport");
            go.transform.SetParent(scrollView.transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.StretchParent();

            var image = go.AddComponent<Image>();
            image.raycastTarget = true;

            var mask = go.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            return go;
        }

        public GameObject CreateContent(GameObject viewport)
        {
            var go = new GameObject("Content");
            go.transform.SetParent(viewport.transform, false);

            var rect = go.AddComponent<RectTransform>();
            rect.StretchTop();

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = _style.RowSpacing;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fit = go.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return go;
        }

        public void Bind(IAtomAnimationClip clip)
        {
            // TODO: Instead of destroying children, try updating them (dirty + index)
            // TODO: If the current clip doesn't change, do not rebind
            Unbind();

            _clip = clip;
            _scrubberMax = _clip.AnimationLength;
            _clip.AnimationLengthUpdated += AnimationUpdated;
            foreach (var group in _clip.GetTargetGroups())
            {
                if (group.Count > 0)
                {
                    CreateHeader(group.Label);

                    foreach (var target in group.GetTargets())
                        CreateRow(target);
                }
            }
        }

        private void Unbind()
        {
            _keyframesRows.Clear();
            if (_clip != null) _clip.AnimationLengthUpdated -= AnimationUpdated;
            while (_layout.transform.childCount > 0)
            {
                var child = _layout.transform.GetChild(0);
                child.transform.parent = null;
                Destroy(child);
            }
        }

        private void AnimationUpdated(object sender, EventArgs e)
        {
            // TODO: Implement diff or version number
            _scrubberMax = (sender as AtomAnimationClip).AnimationLength;
        }

        private void CreateHeader(string title)
        {
            var go = new GameObject("Header");
            go.transform.SetParent(_layout.transform, false);

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = _style.RowHeight;

            {
                var child = new GameObject();
                child.transform.SetParent(go.transform, false);

                var rect = child.AddComponent<RectTransform>();
                rect.StretchParent();

                var image = child.AddComponent<GradientImage>();
                image.top = _style.GroupBackgroundColorTop;
                image.bottom = _style.GroupBackgroundColorBottom;
                image.raycastTarget = false;
            }

            {
                var child = new GameObject();
                child.transform.SetParent(go.transform, false);

                var rect = child.AddComponent<RectTransform>();
                rect.StretchParent();
                rect.offsetMin = new Vector2(6f, 0);
                rect.offsetMax = new Vector2(-6f, 0);

                var text = child.AddComponent<Text>();
                text.text = title;
                text.font = _style.Font;
                text.fontSize = 24;
                text.color = _style.FontColor;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleLeft;
                text.raycastTarget = false;
            }
        }

        private void CreateRow(IAtomAnimationTarget target)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(_layout.transform, false);

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = _style.RowHeight;

            DopeSheetKeyframes keyframes = null;

            {
                var child = new GameObject();
                child.transform.SetParent(go.transform, false);

                var rect = child.AddComponent<RectTransform>();
                rect.StretchLeft();
                rect.anchoredPosition = new Vector2(_style.LabelWidth / 2f, 0);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _style.LabelWidth);

                var image = child.AddComponent<GradientImage>();
                image.top = _style.LabelBackgroundColorTop;
                image.bottom = _style.LabelBackgroundColorBottom;
                image.raycastTarget = true;

                var listener = child.AddComponent<Listener>();
                listener.Bind(
                    () =>
                    {
                        if (target.Selected)
                        {
                            keyframes.selected = true;
                            image.top = _style.LabelBackgroundColorTopSelected;
                            image.bottom = _style.LabelBackgroundColorBottomSelected;
                        }
                        else
                        {
                            keyframes.selected = false;
                            image.top = _style.LabelBackgroundColorTop;
                            image.bottom = _style.LabelBackgroundColorBottom;
                        }
                    },
                    handler => { target.SelectedChanged += handler; },
                    handler => { target.SelectedChanged -= handler; }
                );

                var click = child.AddComponent<Clickable>();
                click.onClick.AddListener(() =>
                {
                    // TODO: Highlight row
                    // TODO: Unselect
                    target.Selected = !target.Selected;
                    // TODO: AnimationFrameUpdated
                    // TODO: Highlight the row keyframes
                });
            }

            {
                var child = new GameObject();
                child.transform.SetParent(go.transform, false);

                var rect = child.AddComponent<RectTransform>();
                var padding = 2f;
                rect.StretchLeft();
                rect.anchoredPosition = new Vector2(_style.LabelWidth / 2f, 0);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _style.LabelWidth - padding * 2);

                var text = child.AddComponent<Text>();
                text.text = target.GetShortName();
                text.font = _style.Font;
                text.fontSize = 20;
                text.color = _style.FontColor;
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.resizeTextForBestFit = false; // Better but ugly if true
                text.raycastTarget = false;
            }

            {
                var child = new GameObject();
                child.transform.SetParent(go.transform, false);

                var rect = child.AddComponent<RectTransform>();
                rect.StretchParent();
                rect.anchoredPosition = new Vector2(_style.LabelWidth / 2f, 0);
                rect.sizeDelta = new Vector2(-_style.LabelWidth, 0);

                keyframes = child.AddComponent<DopeSheetKeyframes>();
                keyframes.SetKeyframes(target.GetAllKeyframesTime());
                keyframes.SetTime(0);
                keyframes.style = _style;
                keyframes.raycastTarget = false;
                _keyframesRows.Add(keyframes);
            }
        }

        public void SetScrubberPosition(float val)
        {
            var ratio = Mathf.Clamp(val / _scrubberMax, 0, _scrubberMax);
            _scrubberRect.anchorMin = new Vector2(ratio, 0);
            _scrubberRect.anchorMax = new Vector2(ratio, 1);
            var ms = val.ToMilliseconds();
            foreach(var keyframe in _keyframesRows) keyframe.SetTime(ms);
        }
    }
}