﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Manisero.StreamProcessingModel.Executors;
using Manisero.StreamProcessingModel.Executors.StepExecutorResolvers;
using Manisero.StreamProcessingModel.Models;
using Manisero.StreamProcessingModel.Models.TaskSteps;
using Manisero.StreamProcessingModel.Samples.Utils;
using Xunit;

namespace Manisero.StreamProcessingModel.Samples
{
    public class progress_reporting
    {
        [Theory]
        [InlineData(ResolverType.Sequential)]
        [InlineData(ResolverType.Streaming)]
        public void basic(ResolverType resolverType)
        {
            test(
                TaskExecutorResolvers.Get(resolverType),
                GetBasicStep(),
                new[] { 100 });
        }

        [Theory]
        [InlineData(ResolverType.Sequential)]
        [InlineData(ResolverType.Streaming)]
        public void pipeline(ResolverType resolverType)
        {
            test(
                TaskExecutorResolvers.Get(resolverType),
                GetPipelineStep(3),
                new[] { 33, 66, 100 });
        }

        [Theory]
        [InlineData(ResolverType.Sequential)]
        [InlineData(ResolverType.Streaming)]
        public void pipeline___more_batches_than_expected(ResolverType resolverType)
        {
            test(
                TaskExecutorResolvers.Get(resolverType),
                GetPipelineStep(3, 2),
                new[] { 50, 100, 100 }); // TODO: Consider not reporting unexpected batches
        }

        [Theory]
        [InlineData(ResolverType.Sequential)]
        [InlineData(ResolverType.Streaming)]
        public void pipeline___less_batches_than_expected(ResolverType resolverType)
        {
            test(
                TaskExecutorResolvers.Get(resolverType),
                GetPipelineStep(3, 4),
                new[] { 25, 50, 75 }); // TODO: Consider including step status in TaskProgress (and reporting finished when finished)
        }

        private void test(
            ITaskStepExecutorResolver taskStepExecutorResolver,
            ITaskStep taskStep,
            ICollection<int> expectedProgressReports)
        {
            // Arrange
            var progressReports = new ConcurrentBag<TaskProgress>();

            var taskDescription = new TaskDescription
            {
                Steps = new List<ITaskStep> { taskStep }
            };

            var progress = new Progress<TaskProgress>(x => progressReports.Add(x));

            var cancellationSource = new CancellationTokenSource();
            var executor = new TaskExecutor(taskStepExecutorResolver);

            // Act
            executor.Execute(taskDescription, progress, cancellationSource.Token);

            // Assert
            progressReports.Select(x => x.StepName).Should().OnlyContain(x => x == taskStep.Name);
            progressReports.Select(x => x.ProgressPercentage).ShouldAllBeEquivalentTo(expectedProgressReports);
        }

        private ITaskStep GetBasicStep()
        {
            return BasicTaskStep.Empty("Step");
        }

        private ITaskStep GetPipelineStep(int actualBatchesCount, int? expectedBatchesCount = null)
        {
            return new PipelineTaskStep<int>(
                "Step",
                Enumerable.Repeat<ICollection<int>>(new[] { 0 }, actualBatchesCount),
                expectedBatchesCount ?? actualBatchesCount,
                new List<PipelineBlock<int>>
                {
                    new PipelineBlock<int>(
                        "Block",
                        x => { })
                });
        }
    }
}
