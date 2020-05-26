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
    public class FloatParamAnimationTarget : AnimationTargetBase, IAnimationTargetWithCurves
    {
        public JSONStorable Storable { get; }
        public JSONStorableFloat FloatParam { get; }
        public AnimationCurve Value { get; } = new AnimationCurve();

        public string Name => Storable != null ? $"{Storable.name}/{FloatParam.name}" : FloatParam.name;

        public FloatParamAnimationTarget(JSONStorable storable, JSONStorableFloat jsf)
        {
            Storable = storable;
            FloatParam = jsf;
        }

        public string GetShortName()
        {
            return FloatParam.name;
        }

        public AnimationCurve GetLeadCurve()
        {
            return Value;
        }

        public IEnumerable<AnimationCurve> GetCurves()
        {
            return new[] { Value };
        }

        public void SetKeyframe(float time, float value)
        {
            Value.SetKeyframe(time, value);
            Dirty = true;
        }

        public void DeleteFrame(float time)
        {
            var key = Value.KeyframeBinarySearch(time);
            if (key == -1) return;
            DeleteFrameByKey(key);
        }

        public void DeleteFrameByKey(int key)
        {
            Value.RemoveKey(key);
            Dirty = true;
        }

        public float[] GetAllKeyframesTime()
        {
            var curve = Value;
            var keyframes = new float[curve.length];
            for (var i = 0; i < curve.length; i++)
                keyframes[i] = curve[i].time;
            return keyframes;
        }

        public class Comparer : IComparer<FloatParamAnimationTarget>
        {
            public int Compare(FloatParamAnimationTarget t1, FloatParamAnimationTarget t2)
            {
                return t1.Name.CompareTo(t2.Name);

            }
        }
    }
}
