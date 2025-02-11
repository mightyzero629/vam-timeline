﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VamTimeline
{
    public class ImportOperationTests : ITestClass
    {
        public IEnumerable<Test> GetTests()
        {
            yield return new Test(nameof(CanImport_PerfectMatch_ExistingSegment), CanImport_PerfectMatch_ExistingSegment);
            yield return new Test(nameof(CanImport_PerfectMatch_NewSegment), CanImport_PerfectMatch_NewSegment);
            yield return new Test(nameof(CanImport_PartialMismatch_NewSegment), CanImport_PartialMismatch_NewSegment);
            yield return new Test(nameof(CanImport_FullMismatch_NewSegment), CanImport_FullMismatch_NewSegment);
            yield return new Test(nameof(CanImport_FullMismatch_ExistingSegment), CanImport_FullMismatch_ExistingSegment);
            yield return new Test(nameof(CanImport_Conflict_SegmentName), CanImport_Conflict_SegmentName);
            yield return new Test(nameof(CanImport_Conflict_LayerName), CanImport_Conflict_LayerName);
            yield return new Test(nameof(CanImport_Conflict_AnimName), CanImport_Conflict_AnimName);
            yield return new Test(nameof(CanImport_Source_SharedSegment), CanImport_Source_SharedSegment);
            yield return new Test(nameof(CanImport_Source_NewSharedSegment), CanImport_Source_NewSharedSegment);
            yield return new Test(nameof(CanImport_Conflict_SharedSegment), CanImport_Conflict_SharedSegment);
            yield return new Test(nameof(CanImport_LegacyClip), CanImport_LegacyClip);
        }

        private IEnumerable CanImport_PerfectMatch_ExistingSegment(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer 1", "Segment 1", context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            ctx.segmentJSON.val = "Segment 1";

            context.Assert(ctx.okJSON.val, true, "OK");
            context.Assert(ctx.nameJSON.val, "Anim IMPORTED", "Name");
            context.Assert(ctx.layerJSON.val, "Layer 1", "Layer");
            context.Assert(ctx.layerJSON.choices, new[] { "Layer 1" }, "Layer choices");
            context.Assert(ctx.segmentJSON.val, "Segment 1", "Segment");
            context.Assert(ctx.segmentJSON.choices, new[] { "Segment 1", "Segment IMPORTED" }, "Segment choices");
            context.Assert(ctx.statusJSON.val, @"Ready to import.", "Status");

            ctx.ImportClip();

            context.Assert(ctx.clip.animationSegment, "Segment 1", "Processed segment name");
            context.Assert(ctx.clip.animationLayer, "Layer 1", "Processed layer name");
            context.Assert(ctx.clip.animationName, "Anim IMPORTED", "Processed animation name");
            AtomAnimationsClipsIndex.IndexedSegment segmentIndex;
            List<AtomAnimationClip> layerClips = null;
            context.Assert(context.animation.index.segmentNames, new[] { "Segment 1" }, "Segments once imported");
            context.Assert(context.animation.index.segmentsById.TryGetValue("Segment 1".ToId(), out segmentIndex), true, "Segment imported exists");
            context.Assert(segmentIndex?.layerNames, new[] { "Layer 1" }, "Layers once imported");
            context.Assert(segmentIndex?.layersMapById.TryGetValue("Layer 1".ToId(), out layerClips), true, "Layer imported exists");
            context.Assert(layerClips?.Select(c => c.animationName), new[] { "Anim 1", "Anim IMPORTED" }, "Animations once imported");

            yield break;
        }

        private IEnumerable CanImport_PerfectMatch_NewSegment(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer 1", "Segment 1", context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            context.Assert(ctx.okJSON.val, true, "OK");
            context.Assert(ctx.nameJSON.val, "Anim IMPORTED", "Name");
            context.Assert(ctx.layerJSON.val, "Layer IMPORTED", "Layer");
            context.Assert(ctx.layerJSON.choices, new[] { "Layer IMPORTED" }, "Layer choices");
            context.Assert(ctx.segmentJSON.val, "Segment IMPORTED", "Segment");
            context.Assert(ctx.segmentJSON.choices, new[] { "Segment 1", "Segment IMPORTED" }, "Segment choices");
            context.Assert(ctx.statusJSON.val, @"Ready to import.", "Status");

            ctx.ImportClip();

            context.Assert(ctx.clip.animationSegment, "Segment IMPORTED", "Processed segment name");
            context.Assert(ctx.clip.animationLayer, "Layer IMPORTED", "Processed layer name");
            context.Assert(ctx.clip.animationName, "Anim IMPORTED", "Processed animation name");
            AtomAnimationsClipsIndex.IndexedSegment segmentIndex;
            List<AtomAnimationClip> layerClips = null;
            context.Assert(context.animation.index.segmentNames, new[] { "Segment 1", "Segment IMPORTED" }, "Segments once imported");
            context.Assert(context.animation.index.segmentsById.TryGetValue("Segment IMPORTED".ToId(), out segmentIndex), true, "Segment imported exists");
            context.Assert(segmentIndex?.layerNames, new[] { "Layer IMPORTED" }, "Layers once imported");
            context.Assert(segmentIndex?.layersMapById.TryGetValue("Layer IMPORTED".ToId(), out layerClips), true, "Layer imported exists");
            context.Assert(layerClips?.Select(c => c.animationName), new[] { "Anim IMPORTED" }, "Animations once imported");

            yield break;
        }

        private IEnumerable CanImport_PartialMismatch_NewSegment(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer 1", "Segment 1", context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F2")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T2")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            context.Assert(ctx.okJSON.val, true, "OK");
            context.Assert(ctx.nameJSON.val, "Anim IMPORTED", "Name");
            context.Assert(ctx.layerJSON.val, "Layer IMPORTED", "Layer");
            context.Assert(ctx.layerJSON.choices, new[] { "Layer IMPORTED" }, "Layer choices");
            context.Assert(ctx.segmentJSON.val, "Segment IMPORTED", "Segment");
            context.Assert(ctx.segmentJSON.choices, new[] { "Segment IMPORTED" }, "Segment choices");
            context.Assert(ctx.statusJSON.val, @"Ready to import.", "Status");

            ctx.ImportClip();

            context.Assert(ctx.clip.animationSegment, "Segment IMPORTED", "Processed segment name");
            context.Assert(ctx.clip.animationLayer, "Layer IMPORTED", "Processed layer name");
            context.Assert(ctx.clip.animationName, "Anim IMPORTED", "Processed animation name");
            AtomAnimationsClipsIndex.IndexedSegment segmentIndex;
            List<AtomAnimationClip> layerClips = null;
            context.Assert(context.animation.index.segmentNames, new[] { "Segment 1", "Segment IMPORTED" }, "Segments once imported");
            context.Assert(context.animation.index.segmentsById.TryGetValue("Segment IMPORTED".ToId(), out segmentIndex), true, "Segment imported exists");
            context.Assert(segmentIndex?.layerNames, new[] { "Layer IMPORTED" }, "Layers once imported");
            context.Assert(segmentIndex?.layersMapById.TryGetValue("Layer IMPORTED".ToId(), out layerClips), true, "Layer imported exists");
            context.Assert(layerClips?.Select(c => c.animationName), new[] { "Anim IMPORTED" }, "Animations once imported");

            yield break;
        }

        private IEnumerable CanImport_FullMismatch_ExistingSegment(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer 1", "Segment 1", context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.Add(helper.GivenFreeController("C2")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F2")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T2")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            context.Assert(ctx.okJSON.val, true, "OK");
            context.Assert(ctx.nameJSON.val, "Anim IMPORTED", "Name");
            context.Assert(ctx.layerJSON.val, "Layer IMPORTED", "Layer");
            context.Assert(ctx.layerJSON.choices, new[] { "Layer IMPORTED" }, "Layer choices");
            context.Assert(ctx.segmentJSON.val, "Segment IMPORTED", "Segment");
            context.Assert(ctx.segmentJSON.choices, new[] { "Segment IMPORTED", "Segment 1" }, "Segment choices");
            context.Assert(ctx.statusJSON.val, @"Ready to import.", "Status");

            ctx.segmentJSON.val = "Segment 1";

            context.Assert(ctx.okJSON.val, true, "OK 2");
            context.Assert(ctx.nameJSON.val, "Anim IMPORTED", "Name 2");
            context.Assert(ctx.layerJSON.val, "Layer IMPORTED", "Layer 2");
            context.Assert(ctx.layerJSON.choices, new[] { "Layer IMPORTED" }, "Layer choices 2");
            context.Assert(ctx.segmentJSON.val, "Segment 1", "Segment 2");
            context.Assert(ctx.segmentJSON.choices, new[] { "Segment IMPORTED", "Segment 1" }, "Segment choices 2");
            context.Assert(ctx.statusJSON.val, @"Ready to import.", "Status 2");

            ctx.ImportClip();

            context.Assert(ctx.clip.animationSegment, "Segment 1", "Processed segment name");
            context.Assert(ctx.clip.animationLayer, "Layer IMPORTED", "Processed layer name");
            context.Assert(ctx.clip.animationName, "Anim IMPORTED", "Processed animation name");
            AtomAnimationsClipsIndex.IndexedSegment segmentIndex;
            List<AtomAnimationClip> layerClips = null;
            context.Assert(context.animation.index.segmentNames, new[] { "Segment 1" }, "Segments once imported");
            context.Assert(context.animation.index.segmentsById.TryGetValue("Segment 1".ToId(), out segmentIndex), true, "Segment imported exists");
            context.Assert(segmentIndex?.layerNames, new[] { "Layer 1", "Layer IMPORTED" }, "Layers once imported");
            context.Assert(segmentIndex?.layersMapById.TryGetValue("Layer IMPORTED".ToId(), out layerClips), true, "Layer imported exists");
            context.Assert(layerClips?.Select(c => c.animationName), new[] { "Anim IMPORTED" }, "Animations once imported");

            yield break;
        }

        private IEnumerable CanImport_FullMismatch_NewSegment(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer 1", "Segment 1", context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.Add(helper.GivenFreeController("C2")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F2")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T2")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            context.Assert(ctx.okJSON.val, true, "OK");
            context.Assert(ctx.nameJSON.val, "Anim IMPORTED", "Name");
            context.Assert(ctx.layerJSON.val, "Layer IMPORTED", "Layer");
            context.Assert(ctx.layerJSON.choices, new[] { "Layer IMPORTED" }, "Layer choices");
            context.Assert(ctx.segmentJSON.val, "Segment IMPORTED", "Segment");
            context.Assert(ctx.segmentJSON.choices, new[] { "Segment IMPORTED", "Segment 1" }, "Segment choices");
            context.Assert(ctx.statusJSON.val, @"Ready to import.", "Status");

            ctx.ImportClip();

            context.Assert(ctx.clip.animationSegment, "Segment IMPORTED", "Processed segment name");
            context.Assert(ctx.clip.animationLayer, "Layer IMPORTED", "Processed layer name");
            context.Assert(ctx.clip.animationName, "Anim IMPORTED", "Processed animation name");
            AtomAnimationsClipsIndex.IndexedSegment segmentIndex;
            List<AtomAnimationClip> layerClips = null;
            context.Assert(context.animation.index.segmentNames, new[] { "Segment 1", "Segment IMPORTED" }, "Segments once imported");
            context.Assert(context.animation.index.segmentsById.TryGetValue("Segment IMPORTED".ToId(), out segmentIndex), true, "Segment imported exists");
            context.Assert(segmentIndex?.layerNames, new[] { "Layer IMPORTED" }, "Layers once imported");
            context.Assert(segmentIndex?.layersMapById.TryGetValue("Layer IMPORTED".ToId(), out layerClips), true, "Layer imported exists");
            context.Assert(layerClips?.Select(c => c.animationName), new[] { "Anim IMPORTED" }, "Animations once imported");

            yield break;
        }

        private IEnumerable CanImport_Conflict_SegmentName(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer 1", "Segment IMPORTED", context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F2")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T2")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            context.Assert(ctx.okJSON.val, true, "OK");
            context.Assert(ctx.nameJSON.val, "Anim IMPORTED", "Name");
            context.Assert(ctx.layerJSON.val, "Layer IMPORTED", "Layer");
            context.Assert(ctx.layerJSON.choices, new[] { "Layer IMPORTED" }, "Layer choices");
            context.Assert(ctx.segmentJSON.val, "Segment IMPORTED 2", "Segment");
            context.Assert(ctx.segmentJSON.choices, new[] { "Segment IMPORTED 2" }, "Segment choices");
            context.Assert(ctx.statusJSON.val, @"Ready to import.", "Status");

            yield break;
        }

        private IEnumerable CanImport_Conflict_LayerName(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer IMPORTED", "Segment 1", context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.Add(helper.GivenFreeController("C2")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F2")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T2")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            ctx.segmentJSON.val = "Segment 1";

            context.Assert(ctx.segmentJSON.val, "Segment IMPORTED", "Segment");

            yield break;
        }

        private IEnumerable CanImport_Conflict_AnimName(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim IMPORTED", "Layer 1", "Segment 1", context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            ctx.segmentJSON.val = "Segment 1";

            context.Assert(ctx.segmentJSON.val, "Segment 1", "Segment");
            context.Assert(ctx.okJSON.val, false, "Ok");
            context.Assert(ctx.statusJSON.val, "Animation name not available on layer.", "Status");

            yield break;
        }

        private IEnumerable CanImport_Source_SharedSegment(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer 1", AtomAnimationClip.SharedAnimationSegment, context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.animationSegment = AtomAnimationClip.SharedAnimationSegment;
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            context.Assert(ctx.okJSON.val, true, "OK");
            context.Assert(ctx.nameJSON.val, "Anim IMPORTED", "Name");
            context.Assert(ctx.layerJSON.val, "Layer 1", "Layer");
            context.Assert(ctx.layerJSON.choices, new[] { "Layer 1" }, "Layer choices");
            context.Assert(ctx.segmentJSON.val, AtomAnimationClip.SharedAnimationSegment, "Segment");
            context.Assert(ctx.segmentJSON.choices, new[] { AtomAnimationClip.SharedAnimationSegment }, "Segment choices");
            context.Assert(ctx.statusJSON.val, @"Ready to import.", "Status");

            ctx.ImportClip();

            context.Assert(ctx.clip.animationSegment, AtomAnimationClip.SharedAnimationSegment, "Processed segment name");
            context.Assert(ctx.clip.animationLayer, "Layer 1", "Processed layer name");
            context.Assert(ctx.clip.animationName, "Anim IMPORTED", "Processed animation name");
            AtomAnimationsClipsIndex.IndexedSegment segmentIndex;
            List<AtomAnimationClip> layerClips = null;
            context.Assert(context.animation.index.segmentNames, new[] { AtomAnimationClip.SharedAnimationSegment }, "Segments once imported");
            context.Assert(context.animation.index.segmentsById.TryGetValue(AtomAnimationClip.SharedAnimationSegmentId, out segmentIndex), true, "Segment imported exists");
            context.Assert(segmentIndex?.layerNames, new[] { "Layer 1" }, "Layers once imported");
            context.Assert(segmentIndex?.layersMapById.TryGetValue("Layer 1".ToId(), out layerClips), true, "Layer imported exists");
            context.Assert(layerClips?.Select(c => c.animationName), new[] { "Anim 1", "Anim IMPORTED" }, "Animations once imported");

            yield break;
        }

        private IEnumerable CanImport_Source_NewSharedSegment(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer 1", "Segment 1", context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.animationSegment = AtomAnimationClip.SharedAnimationSegment;
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F2")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T2")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            context.Assert(ctx.okJSON.val, true, "OK");
            context.Assert(ctx.nameJSON.val, "Anim IMPORTED", "Name");
            context.Assert(ctx.layerJSON.val, "Layer IMPORTED", "Layer");
            context.Assert(ctx.layerJSON.choices, new[] { "Layer IMPORTED" }, "Layer choices");
            context.Assert(ctx.segmentJSON.val, AtomAnimationClip.SharedAnimationSegment, "Segment");
            context.Assert(ctx.segmentJSON.choices, new[] { AtomAnimationClip.SharedAnimationSegment }, "Segment choices");
            context.Assert(ctx.statusJSON.val, @"Ready to import.", "Status");

            ctx.ImportClip();

            context.Assert(ctx.clip.animationSegment, AtomAnimationClip.SharedAnimationSegment, "Processed segment name");
            context.Assert(ctx.clip.animationLayer, "Layer IMPORTED", "Processed layer name");
            context.Assert(ctx.clip.animationName, "Anim IMPORTED", "Processed animation name");
            AtomAnimationsClipsIndex.IndexedSegment segmentIndex;
            List<AtomAnimationClip> layerClips = null;
            context.Assert(context.animation.index.segmentNames, new[] { AtomAnimationClip.SharedAnimationSegment, "Segment 1" }, "Segments once imported");
            context.Assert(context.animation.index.segmentsById.TryGetValue(AtomAnimationClip.SharedAnimationSegmentId, out segmentIndex), true, "Segment imported exists");
            context.Assert(segmentIndex?.layerNames, new[] { "Layer IMPORTED" }, "Layers once imported");
            context.Assert(segmentIndex?.layersMapById.TryGetValue("Layer IMPORTED".ToId(), out layerClips), true, "Layer imported exists");
            context.Assert(layerClips?.Select(c => c.animationName), new[] { "Anim IMPORTED" }, "Animations once imported");

            yield break;
        }

        private IEnumerable CanImport_Conflict_SharedSegment(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer 1", AtomAnimationClip.SharedAnimationSegment, context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            context.Assert(ctx.okJSON.val, false, "Ok");
            context.Assert(ctx.statusJSON.val, "Targets reserved by shared segment.\r\nTriggers: T1\r\nControl: C1\r\nFloat Param: F1 Test\r\n", "Status");

            yield break;
        }

        private IEnumerable CanImport_LegacyClip(TestContext context)
        {
            var helper = new TargetsHelper(context);
            context.animation.RemoveClip(context.animation.clips[0]);
            {
                var clip = new AtomAnimationClip("Anim 1", "Layer 1", "Segment 1", context.logger);
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                context.animation.AddClip(clip);
            }
            ImportOperationClip ctx;
            {
                var clip = GivenImportedClip(context);
                clip.animationSegment = AtomAnimationClip.NoneAnimationSegment;
                clip.Add(helper.GivenFreeController("C1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenFloatParam("F1")).AddEdgeFramesIfMissing(clip.animationLength);
                clip.Add(helper.GivenTriggers(clip.animationLayerQualifiedId, "T1")).AddEdgeFramesIfMissing(clip.animationLength);
                ctx = new ImportOperationClip(context.animation, clip);
            }

            ctx.segmentJSON.val = "Segment 2";

            context.Assert(ctx.segmentJSON.val, "Segment 2", "Segment");
            context.Assert(ctx.segmentJSON.choices, new[] { "Segment 1", "Segment 2" }, "Segment choices");

            ctx.ImportClip();

            context.Assert(ctx.clip.animationSegment, "Segment 2", "Processed segment name");

            yield break;
        }

        private AtomAnimationClip GivenImportedClip(TestContext context)
        {
            var clip = new AtomAnimationClip("Anim IMPORTED", "Layer IMPORTED", "Segment IMPORTED", context.logger);
            return clip;
        }
    }
}
