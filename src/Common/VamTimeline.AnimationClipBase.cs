using System;
using System.Collections.Generic;
using System.Linq;

namespace VamTimeline
{

    /// <summary>
    /// VaM Timeline
    /// By Acidbubbles
    /// Animation timeline with keyframes
    /// Source: https://github.com/acidbubbles/vam-timeline
    /// </summary>
    public class AnimationClipBase<TTarget>
        where TTarget : class, IAnimationTarget
    {
        private float _animationLength = 5f;
        public readonly List<TTarget> Targets = new List<TTarget>();
        private TTarget _selected;

        public string AnimationName { get; }

        public float Speed { get; set; } = 1f;

        public float AnimationLength
        {
            get
            {
                return _animationLength;
            }
            set
            {
                if (value == _animationLength)
                    return;
                _animationLength = value;
                foreach (var target in Targets)
                {
                    target.SetLength(value);
                }
            }
        }

        public AnimationClipBase(string animationName)
        {
            AnimationName = animationName;
        }

        public bool IsEmpty()
        {
            return Targets.Count == 0;
        }

        public void SelectTargetByName(string val)
        {
            _selected = string.IsNullOrEmpty(val)
                ? null
                : Targets.FirstOrDefault(c => c.Name == val);
        }

        public IEnumerable<string> GetTargetsNames()
        {
            return Targets.Select(c => c.Name).ToList();
        }

        public float GetNextFrame(float time)
        {
            var nextTime = AnimationLength;
            foreach (var controller in GetAllOrSelectedTargets())
            {
                var targetNextTime = controller.GetCurves().First().keys.FirstOrDefault(k => k.time > time).time;
                if (targetNextTime != 0 && targetNextTime < nextTime) nextTime = targetNextTime;
            }
            if (nextTime == AnimationLength)
                return 0f;
            else
                return nextTime;
        }

        public float GetPreviousFrame(float time)
        {
            if (time == 0f)
                return GetAllOrSelectedTargets().SelectMany(c => c.GetCurves()).SelectMany(c => c.keys).Select(c => c.time).Where(t => t != AnimationLength).Max();
            var previousTime = 0f;
            foreach (var controller in GetAllOrSelectedTargets())
            {
                var targetPreviousTime = controller.GetCurves().First().keys.LastOrDefault(k => k.time < time).time;
                if (targetPreviousTime != 0 && targetPreviousTime > previousTime) previousTime = targetPreviousTime;
            }
            return previousTime;
        }

        public void DeleteFrame(float time)
        {
            foreach (var target in GetAllOrSelectedTargets())
            {
                foreach (var curve in target.GetCurves())
                {
                    var key = Array.FindIndex(curve.keys, k => k.time == time);
                    if (key != -1) curve.RemoveKey(key);
                }
            }
        }

        public IEnumerable<TTarget> GetAllOrSelectedTargets()
        {
            if (_selected != null) return new[] { _selected };
            return Targets;
        }
    }
}
