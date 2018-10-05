﻿using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Manisero.Navvy.Logging;
using Manisero.Navvy.PipelineProcessing;
using Manisero.Navvy.PipelineProcessing.Models;
using Manisero.Navvy.Reporting;
using Manisero.Navvy.Tests.Utils;
using Xunit;

namespace Manisero.Navvy.Tests
{
    public class reporting
    {
        [Fact]
        public void reporter_creates_and_fills_execution_reports()
        {
            // Arrange
            var task = new TaskDefinition(
                new PipelineTaskStep<int>(
                    "Pipeline1",
                    new[] { 1 },
                    new List<PipelineBlock<int>>()),
                new PipelineTaskStep<int>(
                    "Pipeline2",
                    new[] { 1 },
                    new List<PipelineBlock<int>>()));

            var loggerEvents = TaskExecutionLogger.CreateEvents();
            var reporterEvents = TaskExecutionReporter.CreateEvents();

            // Act
            task.Execute(events: loggerEvents.Concat(reporterEvents).ToArray());

            // Assert
            var reports = task.GetExecutionReports();
            reports.Should().NotBeNull().And.NotBeEmpty();
            reports.Should().Contain(x => x.Name == "Pipeline1_charts.html");
            reports.Should().Contain(x => x.Name == "Pipeline2_charts.html");
        }
    }
}