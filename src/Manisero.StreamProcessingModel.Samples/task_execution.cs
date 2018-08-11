﻿using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Manisero.StreamProcessingModel.Executors;
using Manisero.StreamProcessingModel.Models;
using Manisero.StreamProcessingModel.Models.TaskSteps;
using Manisero.StreamProcessingModel.Samples.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Manisero.StreamProcessingModel.Samples
{
    public class task_execution
    {
        private readonly ITestOutputHelper _output;

        public task_execution(
            ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(ResolverType.Sequential)]
        [InlineData(ResolverType.Streaming)]
        public void test(ResolverType resolverType)
        {
            // Arrange
            var initialized = false;
            var sum = 0;
            var completed = false;

            var taskDescription = new TaskDescription
            {
                Steps = new List<ITaskStep>
                {
                    new BasicTaskStep(
                        "Initialize",
                        () => { initialized = true; }),
                    new PipelineTaskStep<int>(
                        "Pipeline",
                        new[]
                        {
                            new[] { 1, 2, 3 },
                            new[] { 4, 5, 6 }
                        },
                        new List<PipelineBlock<int>>
                        {
                            PipelineBlock<int>.ItemBody(
                                "Sum",
                                x => sum += x),
                            PipelineBlock<int>.BatchBody(
                                "Log",
                                x => _output.WriteLine(string.Join(", ", x)))
                        }),
                    new BasicTaskStep(
                        "Complete",
                        () => { completed = true; })
                }
            };

            var progress = new Progress<TaskProgress>(x => _output.WriteLine($"{x.StepName}: {x.ProgressPercentage}%"));
            var cancellationSource = new CancellationTokenSource();

            var executor = new TaskExecutor(TaskExecutorResolvers.Get(resolverType));

            // Act
            var result = executor.Execute(taskDescription, progress, cancellationSource.Token);

            // Assert
            result.Outcome.Should().Be(TaskOutcome.Successful);
            initialized.Should().Be(true);
            sum.Should().Be(21);
            completed.Should().Be(true);
        }
    }
}
