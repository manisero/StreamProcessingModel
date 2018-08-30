﻿using System;
using System.Collections;
using Manisero.StreamProcessingModel.Core.Models;
using Manisero.StreamProcessingModel.Utils;

namespace Manisero.StreamProcessingModel.PipelineProcessing.Events
{
    public struct BatchStartedEvent
    {
        public int BatchNumber;
        public IEnumerable Batch;
        public ITaskStep Step;
        public TaskDescription Task;
        public DateTime Timestamp;
    }

    public struct BatchEndedEvent
    {
        public int BatchNumber;
        public IEnumerable Batch;
        public ITaskStep Step;
        public TaskDescription Task;
        public TimeSpan Duration;
        public DateTime Timestamp;
    }

    public struct BlockStartedEvent
    {
        public IPipelineBlock Block;
        public int BatchNumber;
        public IEnumerable Batch;
        public ITaskStep Step;
        public TaskDescription Task;
        public DateTime Timestamp;
    }

    public struct BlockEndedEvent
    {
        public IPipelineBlock Block;
        public int BatchNumber;
        public IEnumerable Batch;
        public ITaskStep Step;
        public TaskDescription Task;
        public TimeSpan Duration;
        public DateTime Timestamp;
    }

    public class PipelineExecutionEvents : IExecutionEvents
    {
        public event ExecutionEventHandler<BatchStartedEvent> BatchStarted;
        public event ExecutionEventHandler<BatchEndedEvent> BatchEnded;
        public event ExecutionEventHandler<BlockStartedEvent> BlockStarted;
        public event ExecutionEventHandler<BlockEndedEvent> BlockEnded;

        public PipelineExecutionEvents(
            ExecutionEventHandler<BatchStartedEvent> batchStarted = null,
            ExecutionEventHandler<BatchEndedEvent> batchEnded = null,
            ExecutionEventHandler<BlockStartedEvent> blockStarted = null,
            ExecutionEventHandler<BlockEndedEvent> blockEnded = null)
        {
            if (batchStarted != null)
            {
                BatchStarted += batchStarted;
            }

            if (batchEnded != null)
            {
                BatchEnded += batchEnded;
            }

            if (blockStarted != null)
            {
                BlockStarted += blockStarted;
            }

            if (blockEnded != null)
            {
                BlockEnded += blockEnded;
            }
        }

        internal void OnBatchStarted(int batchNumber, IEnumerable batch, ITaskStep step, TaskDescription task)
        {
            BatchStarted?.Invoke(new BatchStartedEvent
            {
                BatchNumber = batchNumber,
                Batch = batch,
                Step = step,
                Task = task,
                Timestamp = DateTimeUtils.Now
            });
        }

        internal void OnBatchEnded(int batchNumber, IEnumerable batch, ITaskStep step, TaskDescription task, TimeSpan duration)
        {
            BatchEnded?.Invoke(new BatchEndedEvent
            {
                BatchNumber = batchNumber,
                Batch = batch,
                Step = step,
                Task = task,
                Duration = duration,
                Timestamp = DateTimeUtils.Now
            });
        }

        internal void OnBlockStarted(IPipelineBlock block, int batchNumber, IEnumerable batch, ITaskStep step, TaskDescription task)
        {
            BlockStarted?.Invoke(new BlockStartedEvent
            {
                Block = block,
                BatchNumber = batchNumber,
                Batch = batch,
                Step = step,
                Task = task,
                Timestamp = DateTimeUtils.Now
            });
        }

        internal void OnBlockEnded(IPipelineBlock block, int batchNumber, IEnumerable batch, ITaskStep step, TaskDescription task, TimeSpan duration)
        {
            BlockEnded?.Invoke(new BlockEndedEvent
            {
                Block = block,
                BatchNumber = batchNumber,
                Batch = batch,
                Step = step,
                Task = task,
                Duration = duration,
                Timestamp = DateTimeUtils.Now
            });
        }
    }
}