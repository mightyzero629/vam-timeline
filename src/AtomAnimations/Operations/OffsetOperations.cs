﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VamTimeline
{
    public class OffsetOperations
    {
        public const string ChangePivotMode = "Change pivot";
        public const string OffsetMode = "Offset";

        private readonly AtomAnimation _animation;
        private readonly AtomAnimationClip _clip;

        public OffsetOperations(AtomAnimation animation, AtomAnimationClip clip)
        {
            _animation = animation;
            _clip = clip;
        }

        public void Apply(AtomClipboardEntry _offsetSnapshot, float from, float to, string offsetMode)
        {
            foreach (var snap in _offsetSnapshot.controllers)
            {
                var target = _clip.targetControllers.First(t => t.controller == snap.controller);
                if (!target.EnsureParentAvailable(false)) continue;
                var rb = target.GetLinkedRigidbody();

                Vector3 pivot;
                Vector3 positionDelta;
                Quaternion rotationDelta;

                {
                    var positionBefore = new Vector3(snap.snapshot.x.value, snap.snapshot.y.value, snap.snapshot.z.value);
                    var rotationBefore = new Quaternion(snap.snapshot.rotX.value, snap.snapshot.rotY.value, snap.snapshot.rotZ.value, snap.snapshot.rotW.value);

                    var positionAfter = rb == null ? snap.controller.control.localPosition : rb.transform.InverseTransformPoint(snap.controller.transform.position);
                    var rotationAfter = rb == null ? snap.controller.control.localRotation : Quaternion.Inverse(rb.rotation) * snap.controller.transform.rotation;

                    pivot = positionBefore;
                    positionDelta = positionAfter - positionBefore;
                    rotationDelta = Quaternion.Inverse(rotationBefore) * rotationAfter;
                }

                foreach (var key in target.GetAllKeyframesKeys())
                {
                    var time = target.GetKeyframeTime(key);
                    if (time < from || time > to) continue;
                    // Do not double-apply
                    if (time == _offsetSnapshot.time) continue;

                    var positionBefore = target.GetKeyframePosition(key);
                    var rotationBefore = target.GetKeyframeRotation(key);

                    if (offsetMode == ChangePivotMode)
                    {
                        var positionAfter = rotationDelta * (positionBefore - pivot) + pivot + positionDelta;
                        target.SetKeyframeByKey(key, positionAfter, rotationDelta * rotationBefore);
                    }
                    else if (offsetMode == OffsetMode)
                    {
                        target.SetKeyframeByKey(key, positionBefore + positionDelta, rotationDelta * rotationBefore);
                    }
                    else
                    {
                        throw new NotImplementedException($"Offset mode '{offsetMode}' is not implemented");
                    }
                }
            }
        }

        internal AtomClipboardEntry Start(float clipTime, IEnumerable<FreeControllerAnimationTarget> enumerable)
        {
            var snapshot = _clip.Copy(clipTime, enumerable.Cast<IAtomAnimationTarget>());
            if (snapshot.controllers.Count == 0)
            {
                SuperController.LogError($"Timeline: Cannot offset, no keyframes were found at time {clipTime}.");
                return null;
            }
            if (snapshot.controllers.Select(c => _clip.targetControllers.First(t => t.controller == c.controller)).Any(t => !t.EnsureParentAvailable(false)))
            {
                return null;
            }
            return snapshot;
        }
    }
}