using System;
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
    public class FreeControllerAnimationTarget : AnimationTargetBase, IAnimationTargetWithCurves
    {
        public FreeControllerV3 Controller;
        public SortedDictionary<int, KeyframeSettings> Settings = new SortedDictionary<int, KeyframeSettings>();
        public AnimationCurve X { get; } = new AnimationCurve();
        public AnimationCurve Y { get; } = new AnimationCurve();
        public AnimationCurve Z { get; } = new AnimationCurve();
        public AnimationCurve RotX { get; } = new AnimationCurve();
        public AnimationCurve RotY { get; } = new AnimationCurve();
        public AnimationCurve RotZ { get; } = new AnimationCurve();
        public AnimationCurve RotW { get; } = new AnimationCurve();
        public List<AnimationCurve> Curves;

        public string Name => Controller.name;

        public FreeControllerAnimationTarget(FreeControllerV3 controller)
        {
            Curves = new List<AnimationCurve> {
                X, Y, Z, RotX, RotY, RotZ, RotW
            };
            Controller = controller;
        }

        public string GetShortName()
        {
            if (Name.EndsWith("Control"))
                return Name.Substring(0, Name.Length - "Control".Length);
            return Name;
        }

        #region Control

        public AnimationCurve GetLeadCurve()
        {
            return X;
        }

        public IEnumerable<AnimationCurve> GetCurves()
        {
            return Curves;
        }

        public void Validate()
        {
            var leadCurve = GetLeadCurve();
            if (leadCurve.length < 2)
            {
                SuperController.LogError($"Target {Name} has {leadCurve.length} frames");
                return;
            }
            if (Settings.Count > leadCurve.length)
            {
                var curveKeys = leadCurve.keys.Select(k => k.time.ToMilliseconds()).ToList();
                var extraneousKeys = Settings.Keys.Except(curveKeys);
                SuperController.LogError($"Target {Name} has {leadCurve.length} frames but {Settings.Count} settings. Attempting auto-repair.");
                foreach (var extraneousKey in extraneousKeys)
                    Settings.Remove(extraneousKey);
            }
            if (Settings.Count != leadCurve.length)
            {
                SuperController.LogError($"Target {Name} has {leadCurve.length} frames but {Settings.Count} settings");
                SuperController.LogError($"  Target  : {string.Join(", ", leadCurve.keys.Select(k => k.time.ToString()).ToArray())}");
                SuperController.LogError($"  Settings: {string.Join(", ", Settings.Select(k => (k.Key / 1000f).ToString()).ToArray())}");
                return;
            }
            var settings = Settings.Select(s => s.Key);
            var keys = leadCurve.keys.Select(k => k.time.ToMilliseconds()).ToArray();
            if (!settings.SequenceEqual(keys))
            {
                SuperController.LogError($"Target {Name} has different times for settings and keyframes");
                SuperController.LogError($"Settings: {string.Join(", ", settings.Select(s => s.ToString()).ToArray())}");
                SuperController.LogError($"Keyframes: {string.Join(", ", keys.Select(k => k.ToString()).ToArray())}");
                return;
            }
        }

        public void ReapplyCurveTypes()
        {
            if (X.length < 2) return;

            foreach (var setting in Settings)
            {
                if (setting.Value.CurveType == CurveTypeValues.LeaveAsIs)
                    continue;

                var time = (setting.Key / 1000f).Snap();
                foreach (var curve in Curves)
                {
                    ApplyCurve(curve, time, setting.Value.CurveType);
                }
            }
        }

        public static void ApplyCurve(AnimationCurve curve, float time, string curveType)
        {
            var key = curve.KeyframeBinarySearch(time);
            if (key == -1) return;
            var keyframe = curve[key];
            var before = key > 0 ? (Keyframe?)curve[key - 1] : null;
            var next = key < curve.length - 1 ? (Keyframe?)curve[key + 1] : null;

            switch (curveType)
            {
                case null:
                case "":
                    return;
                case CurveTypeValues.Flat:
                    keyframe.inTangent = 0f;
                    keyframe.outTangent = 0f;
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.Linear:
                    keyframe.inTangent = AnimationCurveExtensions.CalculateLinearTangent(before, keyframe);
                    keyframe.outTangent = AnimationCurveExtensions.CalculateLinearTangent(keyframe, next);
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.Bounce:
                    keyframe.inTangent = AnimationCurveExtensions.CalculateLinearTangent(before, keyframe);
                    keyframe.outTangent = -keyframe.inTangent;
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.Smooth:
                    curve.SmoothTangents(key, 0f);
                    break;
                case CurveTypeValues.LinearFlat:
                    keyframe.inTangent = AnimationCurveExtensions.CalculateLinearTangent(before, keyframe);
                    keyframe.outTangent = 0f;
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.FlatLinear:
                    keyframe.inTangent = 0f;
                    keyframe.outTangent = AnimationCurveExtensions.CalculateLinearTangent(keyframe, next);
                    curve.MoveKey(key, keyframe);
                    break;
                case CurveTypeValues.CopyPrevious:
                    if (before != null)
                    {
                        keyframe.value = before.Value.value;
                        keyframe.inTangent = 0f;
                        keyframe.outTangent = 0f;
                        curve.MoveKey(key, keyframe);
                    }
                    break;
                default:
                    throw new NotSupportedException($"Curve type {curveType} is not supported");
            }
        }

        public void ReapplyCurvesToClip(AnimationClip clip)
        {
            var path = GetRelativePath();
            clip.SetCurve(path, typeof(Transform), "localPosition.x", X);
            clip.SetCurve(path, typeof(Transform), "localPosition.y", Y);
            clip.SetCurve(path, typeof(Transform), "localPosition.z", Z);
            clip.SetCurve(path, typeof(Transform), "localRotation.x", RotX);
            clip.SetCurve(path, typeof(Transform), "localRotation.y", RotY);
            clip.SetCurve(path, typeof(Transform), "localRotation.z", RotZ);
            clip.SetCurve(path, typeof(Transform), "localRotation.w", RotW);
        }

        public void SmoothLoop()
        {
            foreach (var curve in Curves)
            {
                curve.SmoothLoop();
            }
        }

        private string GetRelativePath()
        {
            // TODO: This is probably what breaks animations with parenting
            var root = Controller.containingAtom.transform;
            var target = Controller.transform;
            var parts = new List<string>();
            Transform t = target;
            while (t != root && t != t.root)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        #endregion

        #region Keyframes control

        public void SetKeyframeToCurrentTransform(float time)
        {
            SetKeyframe(time, Controller.transform.localPosition, Controller.transform.localRotation);
        }

        public void SetKeyframe(float time, Vector3 localPosition, Quaternion locationRotation)
        {
            X.SetKeyframe(time, localPosition.x);
            Y.SetKeyframe(time, localPosition.y);
            Z.SetKeyframe(time, localPosition.z);
            RotX.SetKeyframe(time, locationRotation.x);
            RotY.SetKeyframe(time, locationRotation.y);
            RotZ.SetKeyframe(time, locationRotation.z);
            RotW.SetKeyframe(time, locationRotation.w);
            var ms = time.ToMilliseconds();
            if (!Settings.ContainsKey(ms))
                Settings[ms] = new KeyframeSettings { CurveType = CurveTypeValues.Smooth };
            Dirty = true;
        }

        public void DeleteFrame(float time)
        {
            var key = GetLeadCurve().KeyframeBinarySearch(time);
            if (key != -1) DeleteFrameByKey(key);
        }

        public void DeleteFrameByKey(int key)
        {
            var settingIndex = Settings.Remove(GetLeadCurve()[key].time.ToMilliseconds());
            foreach (var curve in Curves)
            {
                curve.RemoveKey(key);
            }
            Dirty = true;
        }

        public float[] GetAllKeyframesTime()
        {
            var curve = X;
            var keyframes = new float[curve.length];
            for (var i = 0; i < curve.length; i++)
                keyframes[i] = curve[i].time;
            return keyframes;
        }

        #endregion

        #region Curves

        public void ChangeCurve(float time, string curveType)
        {
            if (string.IsNullOrEmpty(curveType)) return;

            UpdateSetting(time, curveType, false);
            Dirty = true;
        }

        #endregion

        #region Snapshots

        public FreeControllerV3Snapshot GetCurveSnapshot(float time)
        {
            if (X.KeyframeBinarySearch(time) == -1) return null;
            KeyframeSettings setting;
            return new FreeControllerV3Snapshot
            {
                X = X[X.KeyframeBinarySearch(time)],
                Y = Y[Y.KeyframeBinarySearch(time)],
                Z = Z[Z.KeyframeBinarySearch(time)],
                RotX = RotX[RotX.KeyframeBinarySearch(time)],
                RotY = RotY[RotY.KeyframeBinarySearch(time)],
                RotZ = RotZ[RotZ.KeyframeBinarySearch(time)],
                RotW = RotW[RotW.KeyframeBinarySearch(time)],
                CurveType = Settings.TryGetValue(time.ToMilliseconds(), out setting) ? setting.CurveType : CurveTypeValues.LeaveAsIs
            };
        }

        public void SetCurveSnapshot(float time, FreeControllerV3Snapshot snapshot)
        {
            X.SetKeySnapshot(time, snapshot.X);
            Y.SetKeySnapshot(time, snapshot.Y);
            Z.SetKeySnapshot(time, snapshot.Z);
            RotX.SetKeySnapshot(time, snapshot.RotX);
            RotY.SetKeySnapshot(time, snapshot.RotY);
            RotZ.SetKeySnapshot(time, snapshot.RotZ);
            RotW.SetKeySnapshot(time, snapshot.RotW);
            UpdateSetting(time, snapshot.CurveType, true);
            Dirty = true;
        }

        private void UpdateSetting(float time, string curveType, bool create)
        {
            var ms = time.ToMilliseconds();
            if (Settings.ContainsKey(ms))
                Settings[ms].CurveType = curveType;
            else if (create)
                Settings.Add(ms, new KeyframeSettings { CurveType = curveType });
        }

        #endregion

        #region Interpolation


        public bool Interpolate(float playTime, float maxDistanceDelta, float maxRadiansDelta)
        {
            var targetLocalPosition = new Vector3
            {
                x = X.Evaluate(playTime),
                y = Y.Evaluate(playTime),
                z = Z.Evaluate(playTime)
            };

            var targetLocalRotation = new Quaternion
            {
                x = RotX.Evaluate(playTime),
                y = RotY.Evaluate(playTime),
                z = RotZ.Evaluate(playTime),
                w = RotW.Evaluate(playTime)
            };

            Controller.transform.localPosition = Vector3.MoveTowards(Controller.transform.localPosition, targetLocalPosition, maxDistanceDelta);
            Controller.transform.localRotation = Quaternion.RotateTowards(Controller.transform.localRotation, targetLocalRotation, maxRadiansDelta);

            var posDistance = Vector3.Distance(Controller.transform.localPosition, targetLocalPosition);
            // NOTE: We skip checking for rotation reached because in some cases we just never get even near the target rotation.
            // var rotDistance = Quaternion.Dot(Controller.transform.localRotation, targetLocalRotation);
            return posDistance < 0.01f;
        }

        #endregion

        public class Comparer : IComparer<FreeControllerAnimationTarget>
        {
            public int Compare(FreeControllerAnimationTarget t1, FreeControllerAnimationTarget t2)
            {
                return t1.Controller.name.CompareTo(t2.Controller.name);

            }
        }
    }
}
