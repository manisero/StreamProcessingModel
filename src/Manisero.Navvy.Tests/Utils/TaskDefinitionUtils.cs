﻿using System.Threading;
using Manisero.Navvy.Core;

namespace Manisero.Navvy.Tests.Utils
{
    public static class TaskDefinitionUtils
    {
        public static TaskResult Execute(
            this TaskDefinition task,
            ResolverType resolverType = ResolverType.Sequential,
            CancellationTokenSource cancellation = null,
            params IExecutionEvents[] events)
        {
            var executor = TaskExecutorFactory.Create(resolverType);
            
            return executor.Execute(task, cancellation?.Token, events);
        }
    }
}
