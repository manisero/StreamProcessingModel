﻿using System;
using System.Collections.Generic;
using System.Linq;
using Manisero.Navvy.Logging;
using Manisero.Navvy.Logging.Diagnostics;
using Manisero.Navvy.PipelineProcessing;
using Manisero.Navvy.PipelineProcessing.Models;
using Manisero.Navvy.Reporting.Shared;
using Manisero.Navvy.Reporting.Utils;

namespace Manisero.Navvy.Reporting.PipelineReporting
{
    internal interface IPipelineReportDataExtractor
    {
        PipelineReportData Extract(
            IPipelineTaskStep pipeline,
            TaskExecutionLog log);
    }

    internal class PipelineReportDataExtractor : IPipelineReportDataExtractor
    {
        public PipelineReportData Extract(
            IPipelineTaskStep pipeline,
            TaskExecutionLog log)
        {
            var stepLog = log.StepLogs[pipeline.Name];
            var materializationBlockName = pipeline.Input.Name ?? PipelineInput.DefaultName;
            var blockNames = pipeline.Blocks.Select(x => x.Name).ToArray();

            var diagnosticChartsData = GetDiagnosticChartsData(
                stepLog.Duration,
                log.DiagnosticsLog.GetDiagnostics());

            return new PipelineReportData
            {
                ItemsTimelineData = GetItemsTimelineData(stepLog, materializationBlockName, blockNames).ToArray(),
                BlockTimesData = GetBlockTimesData(stepLog, materializationBlockName, blockNames).ToArray(),
                MemoryData = diagnosticChartsData.Item1,
                CpuUsageData = diagnosticChartsData.Item2
            };
        }

        private IEnumerable<ICollection<object>> GetItemsTimelineData(
            TaskStepLog stepLog,
            string materializationBlockName,
            IEnumerable<string> blockNames)
        {
            var headerRow = new[] { "Item", "Step", "Start" + PipelineReportingUtils.MsUnit, "End" + PipelineReportingUtils.MsUnit };

            var dataRows = stepLog
                .ItemLogs
                .Select(x => GetItemsTimelineItemRows(x.Key, x.Value, materializationBlockName, blockNames, stepLog.Duration.StartTs))
                .SelectMany(rows => rows);

            return headerRow.ToEnumerable().Concat(dataRows);
        }

        private IEnumerable<ICollection<object>> GetItemsTimelineItemRows(
            int itemNumber,
            PipelineItemLog itemLog,
            string materializationBlockName,
            IEnumerable<string> blockNames,
            DateTime stepStartTs)
        {
            var itemNumberString = $"Item {itemNumber}";

            yield return new object[]
            {
                itemNumberString,
                materializationBlockName,
                (itemLog.Duration.StartTs - stepStartTs).GetLogValue(),
                (itemLog.Duration.StartTs + itemLog.MaterializationDuration - stepStartTs).GetLogValue()
            };

            foreach (var blockName in blockNames)
            {
                var blockDuration = itemLog.BlockDurations[blockName];

                yield return new object[]
                {
                    itemNumberString,
                    blockName,
                    (blockDuration.StartTs - stepStartTs).GetLogValue(),
                    (blockDuration.EndTs - stepStartTs).GetLogValue()
                };
            }
        }

        private IEnumerable<ICollection<object>> GetBlockTimesData(
            TaskStepLog stepLog,
            string materializationBlockName,
            IEnumerable<string> blockNames)
        {
            yield return new object[] { "Block", "Total duration" + PipelineReportingUtils.MsUnit };
            yield return new object[] { "[Total]", stepLog.Duration.Duration.GetLogValue() };
            yield return new object[] { materializationBlockName, stepLog.BlockTotals.MaterializationDuration.GetLogValue() };

            foreach (var blockName in blockNames)
            {
                yield return new object[] { blockName, stepLog.BlockTotals.BlockDurations[blockName].GetLogValue() };
            }
        }

        private Tuple<ICollection<ICollection<object>>, ICollection<ICollection<object>>> GetDiagnosticChartsData(
            DurationLog stepDuration,
            IEnumerable<Diagnostic> diagnostics)
        {
            var memoryData = new List<ICollection<object>>
            {
                new[]
                {
                    "Time" + PipelineReportingUtils.MsUnit,
                    "Process working set" + PipelineReportingUtils.MbUnit,
                    "GC allocated set" + PipelineReportingUtils.MbUnit
                }
            };

            var cpuUsageData = new List<ICollection<object>>
            {
                new[]
                {
                    "Time" + PipelineReportingUtils.MsUnit,
                    "Avg CPU usage" + PipelineReportingUtils.PercentUnit
                }
            };

            var relevantDiagnostics = diagnostics
                .Where(x => x.Timestamp.IsBetween(stepDuration.StartTs, stepDuration.EndTs))
                .OrderBy(x => x.Timestamp);

            FillDiagnosticData(
                relevantDiagnostics,
                stepDuration.StartTs,
                memoryData,
                cpuUsageData);

            return new Tuple<ICollection<ICollection<object>>, ICollection<ICollection<object>>>(
                memoryData, cpuUsageData);
        }

        private void FillDiagnosticData(
            IEnumerable<Diagnostic> relevantDiagnosticsByTs,
            DateTime taskStartTs,
            ICollection<ICollection<object>> memoryDataToFill,
            ICollection<ICollection<object>> cpuUsageDataToFill)
        {
            double? prevCpuDiagnosticTime = null;

            foreach (var diagnostic in relevantDiagnosticsByTs)
            {
                var time = (diagnostic.Timestamp - taskStartTs).GetLogValue();

                memoryDataToFill.Add(
                    new object[] { time, diagnostic.ProcessWorkingSet.ToMb(), diagnostic.GcAllocatedSet.ToMb() });

                if (prevCpuDiagnosticTime == null)
                {
                    prevCpuDiagnosticTime = time;
                    continue;
                }

                if (diagnostic.CpuUsage.HasValue)
                {
                    var cpuUsage = diagnostic.CpuUsage.Value.ToPercentage();
                    cpuUsageDataToFill.Add(new object[] { prevCpuDiagnosticTime, cpuUsage });
                    cpuUsageDataToFill.Add(new object[] { time, cpuUsage });

                    prevCpuDiagnosticTime = time;
                }
            }
        }
    }
}
