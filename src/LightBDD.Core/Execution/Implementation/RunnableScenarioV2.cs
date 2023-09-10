﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightBDD.Core.Dependencies;
using LightBDD.Core.ExecutionContext;
using LightBDD.Core.ExecutionContext.Implementation;
using LightBDD.Core.Extensibility;
using LightBDD.Core.Extensibility.Execution;
using LightBDD.Core.Metadata;
using LightBDD.Core.Notification.Events;
using LightBDD.Core.Results;
using LightBDD.Core.Results.Implementation;

namespace LightBDD.Core.Execution.Implementation;

internal class RunnableScenarioV2 : IRunnableScenarioV2, IScenario, IRunStageContext
{
    private readonly ScenarioEntryMethod _entryMethod;
    private readonly Func<Task> _decoratedMethod;
    private readonly ScenarioResult _result;
    private readonly FixtureManager _fixtureManager;
    private readonly ExecutionStatusCollector _collector;
    private IDependencyContainer? _scenarioScope;
    public IScenarioResult Result => _result;
    public IScenarioInfo Info => Result.Info;
    public Func<Exception, bool> ShouldAbortSubStepExecution { get; private set; } = _ => true;
    public IDependencyResolver DependencyResolver => _scenarioScope ?? Engine.DependencyContainer;
    public object Context => Fixture;
    public object Fixture => _fixtureManager.Fixture ?? throw new InvalidOperationException("Fixture not initialized");
    public EngineContext Engine { get; }
    IMetadataInfo IRunStageContext.Info => Info;

    public RunnableScenarioV2(EngineContext engine, IScenarioInfo info, IEnumerable<IScenarioDecorator> decorators, ScenarioEntryMethod entryMethod)
    {
        Engine = engine;
        _entryMethod = entryMethod;
        _fixtureManager = new(engine.FixtureFactory);
        _collector = new(engine.ExceptionFormatter);
        _result = new ScenarioResult(info);
        _decoratedMethod = DecoratingExecutor.DecorateScenario(this, () => AsyncStepSynchronizationContext.Execute(RunScenarioCore), decorators);
    }

    public async Task<IScenarioResult> RunAsync()
    {
        var startTime = Engine.ExecutionTimer.GetTime();
        _scenarioScope = Engine.DependencyContainer.BeginScope();
        try
        {
            SetScenarioContext();
            Engine.ProgressDispatcher.Notify(new ScenarioStarting(startTime, Result.Info));
            await _fixtureManager.InitializeAsync(_result.Info.Parent.FeatureType);
            await _decoratedMethod.Invoke();
        }
        catch (StepExecutionException)
        {
            //will be collected via results
        }
        catch (Exception ex)
        {
            _collector.Capture(ex);
        }

        ResetScenarioContext();
        await CleanupScenario();
        var endTime = Engine.ExecutionTimer.GetTime();
        _result.UpdateTime(endTime.GetExecutionTime(startTime));
        _collector.UpdateResults(_result);
        Engine.ProgressDispatcher.Notify(new ScenarioFinished(endTime, Result));
        return Result;
    }

    private void SetScenarioContext()
    {
        ScenarioExecutionContext.Current = new ScenarioExecutionContext();
        ScenarioExecutionContext.Current.Get<CurrentScenarioProperty>().Scenario = this;
    }

    private void ResetScenarioContext()
    {
        ScenarioExecutionContext.Current = null;
    }

    private async Task CleanupScenario()
    {
        await _fixtureManager.DisposeAsync(_collector);
        await _collector.Capture(DisposeScenarioScope);
    }

    private async Task DisposeScenarioScope()
    {
        try
        {
            if (_scenarioScope != null)
                await _scenarioScope.DisposeAsync();
            _scenarioScope = null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"DI Scope Dispose() failed: {ex.Message}", ex);
        }
    }

    public void ConfigureExecutionAbortOnSubStepException(Func<Exception, bool> shouldAbortExecutionFn)
    {
        ShouldAbortSubStepExecution = shouldAbortExecutionFn;
    }

    private async Task RunScenarioCore()
    {
        var stepsRunner = new StepGroupRunner(this, string.Empty);
        ScenarioExecutionContext.Current.Get<CurrentScenarioProperty>().StepsRunner = stepsRunner;
        try
        {
            await _entryMethod.Invoke(Fixture, stepsRunner);
        }
        finally
        {
            ScenarioExecutionContext.Current.Get<CurrentScenarioProperty>().StepsRunner = null;
            _result.UpdateResultsV2(stepsRunner.GetResults());
        }
    }
}