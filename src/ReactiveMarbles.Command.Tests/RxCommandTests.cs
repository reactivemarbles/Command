// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using ReactiveMarbles.Locator;
using ReactiveMarbles.Mvvm;
using ReactiveUI;
using ReactiveUI.Testing;
using Xunit;

namespace ReactiveMarbles.Command.Tests;

/// <summary>
/// Tests for the RxCommand class.
/// </summary>
public class RxCommandTests
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RxCommandTests"/> class.
    /// </summary>
    public RxCommandTests() => ServiceLocator.Current().AddCoreRegistrations(() =>
        CoreRegistrationBuilder
            .Create()
            .WithMainThreadScheduler(new TestScheduler())
            .WithTaskPoolScheduler(new TestScheduler())
            .WithExceptionHandler(new DebugExceptionHandler())
            .Build());

    /// <summary>
    /// A test that determines whether this instance [can execute changed is available via ICommand].
    /// </summary>
    [Fact]
    public void CanExecuteChangedIsAvailableViaICommand()
    {
        var canExecuteSubject = new Subject<bool>();
        ICommand fixture = RxCommand.Create(() => Observable.Return(Unit.Default), canExecuteSubject, ImmediateScheduler.Instance);
        var canExecuteChanged = new List<bool>();
        fixture.CanExecuteChanged += (s, e) => canExecuteChanged.Add(fixture.CanExecute(null));

        canExecuteSubject.OnNext(true);
        canExecuteSubject.OnNext(false);

        canExecuteChanged
            .Should()
            .HaveCount(2)
            .And
            .Subject
            .Should()
            .SatisfyRespectively(first => first.Should().BeTrue(), second => second.Should().BeFalse());
    }

    /// <summary>
    /// A test that determines whether this instance [can execute is available via ICommand].
    /// </summary>
    [Fact]
    public void CanExecuteIsAvailableViaICommand()
    {
        var canExecuteSubject = new Subject<bool>();
        ICommand fixture = RxCommand.Create(() => Observable.Return(Unit.Default), canExecuteSubject, ImmediateScheduler.Instance);

        Assert.False(fixture.CanExecute(null));

        canExecuteSubject.OnNext(true);
        fixture.CanExecute(null).Should().BeTrue();

        canExecuteSubject.OnNext(false);

        fixture.CanExecute(null).Should().BeFalse();
    }

    /// <summary>
    /// Test that determines whether this instance [can execute is behavioral].
    /// </summary>
    [Fact]
    public void CanExecuteIsBehavioral()
    {
        var fixture = RxCommand.Create(() => Observable.Return(Unit.Default), outputScheduler: ImmediateScheduler.Instance);
        fixture.CanCommandExecute.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var canExecute).Subscribe();

        canExecute.Should().ContainSingle(x => x);
    }

    /// <summary>
    /// Test that determines whether this instance [can execute is false if already executing].
    /// </summary>
    [Fact]
    public void CanExecuteIsFalseIfAlreadyExecuting() =>
        new TestScheduler().With(
            scheduler =>
            {
                var execute = Observable.Return(Unit.Default).Delay(TimeSpan.FromSeconds(1), scheduler);
                var fixture = RxCommand.Create(() => execute, outputScheduler: scheduler);
                fixture.CanCommandExecute.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var canExecute)
                    .Subscribe();

                fixture.Execute().Subscribe();
                scheduler.AdvanceByMs(100);

                canExecute.Should().HaveCount(2);
                canExecute[1].Should().BeFalse();

                scheduler.AdvanceByMs(901);

                canExecute.Should().HaveCount(3);
                canExecute[2].Should().BeTrue();
            });

    /// <summary>
    /// Test that determines whether this instance [can execute is false if caller dictates as such].
    /// </summary>
    [Fact]
    public void CanExecuteIsFalseIfCallerDictatesAsSuch()
    {
        var canExecuteSubject = new Subject<bool>();
        var fixture = RxCommand.Create(() => Observable.Return(Unit.Default), canExecuteSubject, ImmediateScheduler.Instance);
        fixture.CanCommandExecute.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var canExecute).Subscribe();

        canExecuteSubject.OnNext(true);
        canExecuteSubject.OnNext(false);

        canExecute
            .Should()
            .HaveCount(3)
            .And
            .Subject
            .Should()
            .SatisfyRespectively(
                first => first.Should()
                    .BeFalse(),
                second => second.Should()
                    .BeTrue(),
                third => third.Should()
                    .BeFalse());
    }

    /// <summary>
    /// Test that determines whether this instance [can execute is unsubscribed after command disposal].
    /// </summary>
    [Fact]
    public void CanExecuteIsUnsubscribedAfterCommandDisposal()
    {
        var canExecuteSubject = new Subject<bool>();
        var fixture = RxCommand.Create(() => Observable.Return(Unit.Default), canExecuteSubject, ImmediateScheduler.Instance);

        Assert.True(canExecuteSubject.HasObservers);

        fixture.Dispose();

        canExecuteSubject.HasObservers.Should().BeFalse();
    }

    /// <summary>
    /// Test that determines whether this instance [can execute only ticks distinct values].
    /// </summary>
    [Fact]
    public void CanExecuteOnlyTicksDistinctValues()
    {
        var canExecuteSubject = new Subject<bool>();
        var fixture = RxCommand.Create(() => Observable.Return(Unit.Default), canExecuteSubject, ImmediateScheduler.Instance);
        fixture.CanCommandExecute.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var canExecute).Subscribe();

        canExecuteSubject.OnNext(false);
        canExecuteSubject.OnNext(false);
        canExecuteSubject.OnNext(false);
        canExecuteSubject.OnNext(false);
        canExecuteSubject.OnNext(true);
        canExecuteSubject.OnNext(true);

        canExecute
            .Should()
            .HaveCount(2)
            .And.Subject.Should()
            .SatisfyRespectively(
                first => first.Should()
                    .BeFalse(),
                second => second.Should()
                    .BeTrue());
    }

    /// <summary>
    /// Test that determines whether this instance [can execute ticks failures through thrown exceptions].
    /// </summary>
    [Fact]
    public void CanExecuteTicksFailuresThroughThrownExceptions()
    {
        var canExecuteSubject = new Subject<bool>();
        var fixture = RxCommand.Create(() => Observable.Return(Unit.Default), canExecuteSubject, ImmediateScheduler.Instance);
        fixture.ThrownExceptions.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var thrownExceptions)
            .Subscribe();

        canExecuteSubject.OnError(new InvalidOperationException("oops"));

        thrownExceptions.Should().ContainSingle(x => x.Message == "oops");
    }

    /// <summary>
    /// Creates the task facilitates TPL integration.
    /// </summary>
    [Fact]
    public void CreateTaskFacilitatesTPLIntegration()
    {
        var fixture = RxCommand.Create<Unit, int>(_ => Task.FromResult(13), outputScheduler: ImmediateScheduler.Instance);
        fixture.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var results).Subscribe();

        fixture.Execute().Subscribe();

        results.Should().ContainSingle(x => x == 13);
    }

    /// <summary>
    /// Creates the task facilitates TPL integration with parameter.
    /// </summary>
    [Fact]
    public void CreateTaskFacilitatesTPLIntegrationWithParameter()
    {
        var fixture = RxCommand.Create<int, int>(param => Task.FromResult(param + 1), outputScheduler: ImmediateScheduler.Instance);
        fixture.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var results).Subscribe();

        fixture.Execute(3).Subscribe();
        fixture.Execute(41).Subscribe();

        results
            .Should()
            .HaveCount(2)
            .And
            .Subject
            .Should()
            .SatisfyRespectively(
                first => first.Should()
                    .Be(4),
                second => second.Should()
                    .Be(42));
    }

    /// <summary>
    /// Creates the throws if execution parameter is null.
    /// </summary>
    [Fact]
    public void CreateThrowsIfExecutionParameterIsNull()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        Assert.Throws<ArgumentNullException>(() => RxCommand.Create((Func<Unit>)null));
        Assert.Throws<ArgumentNullException>(() => RxCommand.Create((Action<Unit>)null));
        Assert.Throws<ArgumentNullException>(() => RxCommand.Create((Func<Unit, Unit>)null));
        Assert.Throws<ArgumentNullException>(() => RxCommand.Create((Func<IObservable<Unit>>)null));
        Assert.Throws<ArgumentNullException>(() => RxCommand.Create((Func<Task<Unit>>)null));
        Assert.Throws<ArgumentNullException>(() => RxCommand.Create((Func<Unit, Task<Unit>>)null));
        Assert.Throws<ArgumentNullException>(() => RxCommand.Create((Func<Unit, CancellationToken, Task<Unit>>)null));
        Assert.Throws<ArgumentNullException>(() => RxCommand.Create((Func<Unit, IObservable<Unit>>)null));
        Assert.Throws<ArgumentNullException>(() => RxCommand.Create((Func<Unit, Task<Unit>>)null));
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    /// <summary>
    /// Exceptions the are delivered on output scheduler.
    /// </summary>
    [Fact]
    public void ExceptionsAreDeliveredOnOutputScheduler() =>
        new TestScheduler().With(
            scheduler =>
            {
                var fixture = RxCommand.Create(() => Observable.Throw<Unit>(new InvalidOperationException()), outputScheduler: scheduler);
                Exception? exception = null;
                fixture.ThrownExceptions.Subscribe(ex => exception = ex);
                fixture.Execute().Subscribe(_ => { }, _ => { });

                Assert.Null(exception);
                scheduler.Start();
                exception.Should().BeOfType<InvalidOperationException>();
            });

    /// <summary>
    /// Executes the can be cancelled.
    /// </summary>
    [Fact]
    public void ExecuteCanBeCancelled() =>
        new TestScheduler().With(
            scheduler =>
            {
                var execute = Observable.Return(Unit.Default).Delay(TimeSpan.FromSeconds(1), scheduler);
                var fixture = RxCommand.Create(() => execute, outputScheduler: scheduler);
                fixture.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var executed).Subscribe();

                var sub1 = fixture.Execute().Subscribe();
                var sub2 = fixture.Execute().Subscribe();
                scheduler.AdvanceByMs(999);

                Assert.True(fixture.IsExecuting.FirstAsync().Wait());
                Assert.Empty(executed);
                sub1.Dispose();

                scheduler.AdvanceByMs(2);
                Assert.Equal(1, executed.Count);
                Assert.False(fixture.IsExecuting.FirstAsync().Wait());
            });

    /// <summary>
    /// Executes the can tick through multiple results.
    /// </summary>
    [Fact]
    public void ExecuteCanTickThroughMultipleResults()
    {
        var fixture = RxCommand.Create(
            () => new[] { 1, 2, 3 }.ToObservable(),
            outputScheduler: ImmediateScheduler.Instance);
        fixture.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var results).Subscribe();

        fixture.Execute().Subscribe();

        results
            .Should()
            .HaveCount(3)
            .And.Subject.Should()
            .SatisfyRespectively(
                first => first.Should()
                    .Be(1),
                second => second.Should()
                    .Be(2),
                third => third.Should()
                    .Be(3));
    }

    /// <summary>
    /// Executes the facilitates any number of in flight executions.
    /// </summary>
    [Fact]
    public void ExecuteFacilitatesAnyNumberOfInFlightExecutions() =>
        new TestScheduler().With(
            scheduler =>
            {
                var execute = Observable.Return(Unit.Default).Delay(TimeSpan.FromMilliseconds(500), scheduler);
                var fixture = RxCommand.Create(() => execute, outputScheduler: scheduler);
                fixture.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var executed).Subscribe();

                var sub1 = fixture.Execute().Subscribe();
                var sub2 = fixture.Execute().Subscribe();
                scheduler.AdvanceByMs(100);

                var sub3 = fixture.Execute().Subscribe();
                scheduler.AdvanceByMs(200);
                var sub4 = fixture.Execute().Subscribe();
                scheduler.AdvanceByMs(100);

                Assert.True(fixture.IsExecuting.FirstAsync().Wait());
                Assert.Empty(executed);

                scheduler.AdvanceByMs(101);
                Assert.Equal(2, executed.Count);
                Assert.True(fixture.IsExecuting.FirstAsync().Wait());

                scheduler.AdvanceByMs(200);
                Assert.Equal(3, executed.Count);
                Assert.True(fixture.IsExecuting.FirstAsync().Wait());

                scheduler.AdvanceByMs(100);
                Assert.Equal(4, executed.Count);
                Assert.False(fixture.IsExecuting.FirstAsync().Wait());
            });

    /// <summary>
    /// Executes the is available via ICommand.
    /// </summary>
    [Fact]
    public void ExecuteIsAvailableViaICommand()
    {
        var executed = false;
        ICommand fixture = RxCommand.Create(
            () =>
            {
                executed = true;
                return Observable.Return(Unit.Default);
            },
            outputScheduler: ImmediateScheduler.Instance);

        fixture.Execute(null);
        executed.Should().BeTrue();
    }

    /// <summary>
    /// Executes the passes through parameter.
    /// </summary>
    [Fact]
    public void ExecutePassesThroughParameter()
    {
        var parameters = new List<int>();
        var fixture = RxCommand.Create<int, Unit>(
            param =>
            {
                parameters.Add(param);
                return Observable.Return(Unit.Default);
            },
            outputScheduler: ImmediateScheduler.Instance);

        fixture.Execute(1).Subscribe();
        fixture.Execute(42).Subscribe();
        fixture.Execute(348).Subscribe();

        parameters
            .Should()
            .HaveCount(3)
            .And
            .Subject
            .Should()
            .SatisfyRespectively(
                first => first.Should().Be(1),
                second => second.Should().Be(42),
                third => third.Should().Be(348));
    }

    /// <summary>
    /// Executes the reenables execution even after failure.
    /// </summary>
    [Fact]
    public void ExecuteReenablesExecutionEvenAfterFailure()
    {
        var fixture = RxCommand.Create(() => Observable.Throw<Unit>(new InvalidOperationException("oops")), outputScheduler: ImmediateScheduler.Instance);
        fixture.CanCommandExecute.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var canExecute).Subscribe();
        fixture.ThrownExceptions.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var thrownExceptions)
            .Subscribe();

        fixture.Execute().Subscribe(_ => { }, _ => { });

        Assert.Equal(1, thrownExceptions.Count);
        Assert.Equal("oops", thrownExceptions[0].Message);

        canExecute
            .Should()
            .HaveCount(3)
            .And
            .Subject
            .Should()
            .SatisfyRespectively(
                first => first.Should().BeTrue(),
                second => second.Should().BeFalse(),
                third => third.Should().BeTrue());
    }

    /// <summary>
    /// Executes the result is delivered on specified scheduler.
    /// </summary>
    [Fact]
    public void ExecuteResultIsDeliveredOnSpecifiedScheduler() =>
        new TestScheduler().With(
            scheduler =>
            {
                var execute = Observable.Return(Unit.Default);
                var fixture = RxCommand.Create(() => execute, outputScheduler: scheduler);
                var executed = false;

                fixture.Execute().ObserveOn(scheduler).Subscribe(_ => executed = true);

                executed.Should().BeFalse();
                scheduler.AdvanceByMs(1);
                executed.Should().BeTrue();
            });

    /// <summary>
    /// Executes the ticks any exception.
    /// </summary>
    [Fact]
    public void ExecuteTicksAnyException()
    {
        var fixture = RxCommand.Create(() => Observable.Throw<Unit>(new InvalidOperationException()), outputScheduler: ImmediateScheduler.Instance);
        fixture.ThrownExceptions.Subscribe();
        Exception? exception = null;
        fixture.Execute().Subscribe(_ => { }, ex => exception = ex, () => { });

        exception.Should().BeOfType<InvalidOperationException>();
    }

    /// <summary>
    /// Executes the ticks any lambda exception.
    /// </summary>
    [Fact]
    public void ExecuteTicksAnyLambdaException()
    {
        Unit Execute() => throw new InvalidOperationException();
        var fixture = RxCommand.Create(Execute, outputScheduler: ImmediateScheduler.Instance);
        fixture.ThrownExceptions.Subscribe();
        Exception? exception = null;
        fixture.Execute().Subscribe(_ => { }, ex => exception = ex, () => { });

        exception.Should().BeOfType<InvalidOperationException>();
    }

    /// <summary>
    /// Executes the ticks errors through thrown exceptions.
    /// </summary>
    [Fact]
    public void ExecuteTicksErrorsThroughThrownExceptions()
    {
        var fixture = RxCommand.Create(() => Observable.Throw<Unit>(new InvalidOperationException("oops")), outputScheduler: ImmediateScheduler.Instance);
        fixture.ThrownExceptions.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var thrownExceptions)
            .Subscribe();

        fixture.Execute().Subscribe(_ => { }, _ => { });

        thrownExceptions.Should().ContainSingle(x => x.Message == "oops");
    }

    /// <summary>
    /// Executes the ticks lambda errors through thrown exceptions.
    /// </summary>
    [Fact]
    public void ExecuteTicksLambdaErrorsThroughThrownExceptions()
    {
        Unit Execute() => throw new InvalidOperationException("oops");
        var fixture = RxCommand.Create(Execute, outputScheduler: ImmediateScheduler.Instance);
        fixture.ThrownExceptions.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var thrownExceptions)
            .Subscribe();

        fixture.Execute().Subscribe(_ => { }, _ => { });

        Assert.Equal(1, thrownExceptions.Count);
        Assert.Equal("oops", thrownExceptions[0].Message);
        Assert.True(fixture.CanCommandExecute.FirstAsync().Wait());
    }

    /// <summary>
    /// Executes the ticks through the result.
    /// </summary>
    [Fact]
    public void ExecuteTicksThroughTheResult()
    {
        var num = 0;
        var fixture = RxCommand.Create(() => Observable.Return(num), outputScheduler: ImmediateScheduler.Instance);
        fixture.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var results).Subscribe();

        num = 1;
        fixture.Execute().Subscribe();
        num = 10;
        fixture.Execute().Subscribe();
        num = 30;
        fixture.Execute().Subscribe();

        results
            .Should()
            .HaveCount(3)
            .And
            .Subject
            .Should()
            .SatisfyRespectively(
                first => first.Should().Be(1),
                second => second.Should().Be(10),
                third => third.Should().Be(30));
    }

    /// <summary>
    /// Executes via ICommand throws if parameter type is incorrect.
    /// </summary>
    [Fact]
    public void ExecuteViaICommandThrowsIfParameterTypeIsIncorrect()
    {
        ICommand fixture = RxCommand.Create<int>(_ => { }, outputScheduler: ImmediateScheduler.Instance);
        var ex = Assert.Throws<InvalidOperationException>(() => fixture.Execute("foo"));
        Assert.Equal("Command requires parameters of type System.Int32, but received parameter of type System.String.", ex.Message);

        fixture = RxCommand.Create<string>(_ => { });
        ex = Assert.Throws<InvalidOperationException>(() => fixture.Execute(13));

        ex.Message.Should()
            .Be("Command requires parameters of type System.String, but received parameter of type System.Int32.");
    }

    /// <summary>
    /// Executes via ICommand works with nullable types.
    /// </summary>
    [Fact]
    public void ExecuteViaICommandWorksWithNullableTypes()
    {
        int? value = null;
        ICommand fixture =
            RxCommand.Create<int?>(param => value = param, outputScheduler: ImmediateScheduler.Instance);

        fixture.Execute(42);
        value.Should().Be(42);

        fixture.Execute(null);
        value.Should().BeNull();
    }

    /// <summary>
    /// Test that invokes the command against ICommand in target invokes the command.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandInTargetInvokesTheCommand()
    {
        var executionCount = 0;
        var fixture = new ICommandHolder();
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture, x => x.TheCommand!);
        fixture.TheCommand = RxCommand.Create(() => ++executionCount, outputScheduler: ImmediateScheduler.Instance);

        source.OnNext(Unit.Default);
        executionCount.Should().Be(1);
        Assert.Equal(1, executionCount);

        source.OnNext(Unit.Default);
        executionCount.Should().Be(2);
    }

    /// <summary>
    /// Test that invokes the command against ICommand in target passes the specified value to can execute and execute.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandInTargetPassesTheSpecifiedValueToCanExecuteAndExecute()
    {
        var fixture = new ICommandHolder();
        var source = new Subject<int>();
        source.InvokeCommand(fixture, x => x.TheCommand!);
        var command = new FakeCommand();
        fixture.TheCommand = command;

        source.OnNext(42);
        command.ExecuteParameter.Should().Be(42);
        command.CanExecuteParameter.Should().Be(42);
    }

    /// <summary>
    /// Test that invokes the command against ICommand in target passes the specified value to can execute and execute.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandInNullableTargetPassesTheSpecifiedValueToCanExecuteAndExecute()
    {
        var fixture = new ICommandHolder();
        var source = new Subject<int>();
        source.InvokeCommand(fixture, x => x.TheCommand);
        var command = new FakeCommand();
        fixture.TheCommand = command;

        source.OnNext(42);
        command.ExecuteParameter.Should().Be(42);
        command.CanExecuteParameter.Should().Be(42);
    }

    /// <summary>
    /// Test that invokes the command against i command in target respects can execute.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandInTargetRespectsCanExecute()
    {
        var executed = false;
        var canExecute = new BehaviorSubject<bool>(false);
        var fixture = new ICommandHolder();
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture, x => x.TheCommand!);
        fixture.TheCommand = RxCommand.Create(() => executed = true, canExecute, ImmediateScheduler.Instance);

        source.OnNext(Unit.Default);
        executed.Should().BeFalse();

        canExecute.OnNext(true);
        source.OnNext(Unit.Default);
        executed.Should().BeTrue();
    }

    /// <summary>
    /// Test that invokes the command against i command in target respects can execute.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandInNullableTargetRespectsCanExecute()
    {
        var executed = false;
        var canExecute = new BehaviorSubject<bool>(false);
        var fixture = new ICommandHolder();
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture, x => x.TheCommand);
        fixture.TheCommand = RxCommand.Create(() => executed = true, canExecute, ImmediateScheduler.Instance);

        source.OnNext(Unit.Default);
        executed.Should().BeFalse();

        canExecute.OnNext(true);
        source.OnNext(Unit.Default);
        executed.Should().BeTrue();
    }

    /// <summary>
    /// Test that invokes the command against ICommand in target respects can execute window.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandInTargetRespectsCanExecuteWindow()
    {
        var executed = false;
        var canExecute = new BehaviorSubject<bool>(false);
        var fixture = new ICommandHolder();
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture, x => x.TheCommand!);
        fixture.TheCommand = RxCommand.Create(() => executed = true, canExecute, ImmediateScheduler.Instance);

        source.OnNext(Unit.Default);
        executed.Should().BeFalse();

        // The execution window re-opens, but the above execution request should not be instigated because
        // it occurred when the window was closed. Execution requests do not queue up when the window is closed.
        canExecute.OnNext(true);
        executed.Should().BeFalse();
    }

    /// <summary>
    /// Test that invokes the command against ICommand in target swallows exceptions.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandInTargetSwallowsExceptions()
    {
        var count = 0;
        var fixture = new ICommandHolder();
        var command = RxCommand.Create(
            () =>
            {
                ++count;
                throw new InvalidOperationException();
            },
            outputScheduler: ImmediateScheduler.Instance);
        command.ThrownExceptions.Subscribe();
        fixture.TheCommand = command;
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture, x => x.TheCommand!);

        source.OnNext(Unit.Default);
        source.OnNext(Unit.Default);

        count.Should().Be(2);
    }

    /// <summary>
    /// Test that invokes the command against ICommand invokes the command.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandInvokesTheCommand()
    {
        var executionCount = 0;
        ICommand fixture = RxCommand.Create(() => ++executionCount, outputScheduler: ImmediateScheduler.Instance);
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture);

        source.OnNext(Unit.Default);
        executionCount.Should().Be(1);

        source.OnNext(Unit.Default);
        executionCount.Should().Be(2);
    }

    /// <summary>
    /// Test that invokes the command against ICommand invokes the command.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstNullableICommandInvokesTheCommand()
    {
        var executionCount = 0;
        ICommand fixture = RxCommand.Create(() => ++executionCount, outputScheduler: ImmediateScheduler.Instance);
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture);

        source.OnNext(Unit.Default);
        executionCount.Should().Be(1);

        source.OnNext(Unit.Default);
        executionCount.Should().Be(2);
    }

    /// <summary>
    /// Test that invokes the command against ICommand passes the specified value to can execute and execute.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandPassesTheSpecifiedValueToCanExecuteAndExecute()
    {
        var fixture = new FakeCommand();
        var source = new Subject<int>();
        source.InvokeCommand(fixture);

        source.OnNext(42);

        fixture.ExecuteParameter.Should().Be(42);
        fixture.CanExecuteParameter.Should().Be(42);
    }

    /// <summary>
    /// Test that invokes the command against ICommand respects can execute.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandRespectsCanExecute()
    {
        var executed = false;
        var canExecute = new BehaviorSubject<bool>(false);
        ICommand fixture = RxCommand.Create(() => executed = true, canExecute, ImmediateScheduler.Instance);
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture);

        source.OnNext(Unit.Default);
        executed.Should().BeFalse();

        canExecute.OnNext(true);
        source.OnNext(Unit.Default);
        executed.Should().BeTrue();
    }

    /// <summary>
    /// Test that invokes the command against ICommand respects can execute window.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandRespectsCanExecuteWindow()
    {
        var executed = false;
        var canExecute = new BehaviorSubject<bool>(false);
        ICommand fixture = RxCommand.Create(() => executed = true, canExecute, ImmediateScheduler.Instance);
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture);

        source.OnNext(Unit.Default);
        executed.Should().BeFalse();

        // The execution window re-opens, but the above execution request should not be instigated because
        // it occurred when the window was closed. Execution requests do not queue up when the window is closed.
        canExecute.OnNext(true);
        executed.Should().BeFalse();
    }

    /// <summary>
    /// Test that invokes the command against ICommand swallows exceptions.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstICommandSwallowsExceptions()
    {
        var count = 0;
        var fixture = RxCommand.Create(
            () =>
            {
                ++count;
                throw new InvalidOperationException();
            },
            outputScheduler: ImmediateScheduler.Instance);
        fixture.ThrownExceptions.Subscribe();
        var source = new Subject<Unit>();
        source.InvokeCommand((ICommand)fixture);

        source.OnNext(Unit.Default);
        source.OnNext(Unit.Default);

        count.Should().Be(2);
    }

    /// <summary>
    /// Test that invokes the command against reactive command in target invokes the command.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstRxCommandInTargetInvokesTheCommand()
    {
        var executionCount = 0;
        var fixture = new RxCommandHolder();
        var source = new Subject<int>();
        source.InvokeCommand(fixture, x => x.TheCommand!);
        fixture.TheCommand = RxCommand.Create<int>(_ => ++executionCount, outputScheduler: ImmediateScheduler.Instance);

        source.OnNext(0);
        executionCount.Should().Be(1);

        source.OnNext(0);
        executionCount.Should().Be(2);
    }

    /// <summary>
    /// Test that invokes the command against reactive command in target passes the specified value to execute.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstRxCommandInTargetPassesTheSpecifiedValueToExecute()
    {
        var executeReceived = 0;
        var fixture = new RxCommandHolder();
        var source = new Subject<int>();
        source.InvokeCommand(fixture, x => x.TheCommand!);
        fixture.TheCommand =
            RxCommand.Create<int>(x => executeReceived = x, outputScheduler: ImmediateScheduler.Instance);

        source.OnNext(42);
        executeReceived.Should().Be(42);
    }

    /// <summary>
    /// Test that invokes the command against reactive command in target respects can execute.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstRxCommandInTargetRespectsCanExecute()
    {
        var executed = false;
        var canExecute = new BehaviorSubject<bool>(false);
        var fixture = new RxCommandHolder();
        var source = new Subject<int>();
        source.InvokeCommand(fixture, x => x.TheCommand!);
        fixture.TheCommand = RxCommand.Create<int>(_ => executed = true, canExecute, ImmediateScheduler.Instance);

        source.OnNext(0);
        executed.Should().BeFalse();

        canExecute.OnNext(true);
        source.OnNext(0);
        executed.Should().BeTrue();
    }

    /// <summary>
    /// Test that invokes the command against reactive command in target respects can execute window.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstRxCommandInTargetRespectsCanExecuteWindow()
    {
        var executed = false;
        var canExecute = new BehaviorSubject<bool>(false);
        var fixture = new RxCommandHolder();
        var source = new Subject<int>();
        source.InvokeCommand(fixture, x => x.TheCommand!);
        fixture.TheCommand = RxCommand.Create<int>(_ => executed = true, canExecute, ImmediateScheduler.Instance);

        source.OnNext(0);
        executed.Should().BeFalse();

        // The execution window re-opens, but the above execution request should not be instigated because
        // it occurred when the window was closed. Execution requests do not queue up when the window is closed.
        canExecute.OnNext(true);
        executed.Should().BeFalse();
    }

    /// <summary>
    /// Test that invokes the command against reactive command in target swallows exceptions.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstRxCommandInTargetSwallowsExceptions()
    {
        var count = 0;
        var fixture = new RxCommandHolder()
        {
            TheCommand = RxCommand.Create<int>(
                _ =>
                {
                    ++count;
                    throw new InvalidOperationException();
                },
                outputScheduler: ImmediateScheduler.Instance)
        };
        fixture.TheCommand.ThrownExceptions.Subscribe();
        var source = new Subject<int>();
        source.InvokeCommand(fixture, x => x.TheCommand!);

        source.OnNext(0);
        source.OnNext(0);

        count.Should().Be(2);
    }

    /// <summary>
    /// Test that invokes the command against reactive command invokes the command.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstRxCommandInvokesTheCommand()
    {
        var executionCount = 0;
        var fixture = RxCommand.Create(() => ++executionCount, outputScheduler: ImmediateScheduler.Instance);
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture);

        source.OnNext(Unit.Default);
        executionCount.Should().Be(1);

        source.OnNext(Unit.Default);
        executionCount.Should().Be(2);
    }

    /// <summary>
    /// Test that invokes the command against reactive command passes the specified value to execute.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstRxCommandPassesTheSpecifiedValueToExecute()
    {
        var executeReceived = 0;
        var fixture = RxCommand.Create<int>(x => executeReceived = x, outputScheduler: ImmediateScheduler.Instance);
        var source = new Subject<int>();
        source.InvokeCommand(fixture);

        source.OnNext(42);
        executeReceived.Should().Be(42);
    }

    /// <summary>
    /// Test that invokes the command against reactive command respects can execute.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstRxCommandRespectsCanExecute()
    {
        var executed = false;
        var canExecute = new BehaviorSubject<bool>(false);
        var fixture = RxCommand.Create(() => executed = true, canExecute, ImmediateScheduler.Instance);
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture);

        source.OnNext(Unit.Default);
        executed.Should().BeFalse();

        canExecute.OnNext(true);
        source.OnNext(Unit.Default);
        executed.Should().BeTrue();
    }

    /// <summary>
    /// Test that invokes the command against reactive command respects can execute window.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstRxCommandRespectsCanExecuteWindow()
    {
        var executed = false;
        var canExecute = new BehaviorSubject<bool>(false);
        var fixture = RxCommand.Create(() => executed = true, canExecute, outputScheduler: ImmediateScheduler.Instance);
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture);

        source.OnNext(Unit.Default);
        executed.Should().BeFalse();

        // The execution window re-opens, but the above execution request should not be instigated because
        // it occurred when the window was closed. Execution requests do not queue up when the window is closed.
        canExecute.OnNext(true);
        executed.Should().BeFalse();
    }

    /// <summary>
    /// Test that invokes the command against reactive command swallows exceptions.
    /// </summary>
    [Fact]
    public void InvokeCommandAgainstRxCommandSwallowsExceptions()
    {
        var count = 0;
        var fixture = RxCommand.Create(
            () =>
            {
                ++count;
                throw new InvalidOperationException();
            },
            outputScheduler: ImmediateScheduler.Instance);
        fixture.ThrownExceptions.Subscribe();
        var source = new Subject<Unit>();
        source.InvokeCommand(fixture);

        source.OnNext(Unit.Default);
        source.OnNext(Unit.Default);

        count.Should().Be(2);
    }

    /// <summary>
    /// Test that invokes the command works even if the source is cold.
    /// </summary>
    [Fact]
    public void InvokeCommandWorksEvenIfTheSourceIsCold()
    {
        var executionCount = 0;
        var fixture = RxCommand.Create(() => ++executionCount, outputScheduler: ImmediateScheduler.Instance);
        var source = Observable.Return(Unit.Default);
        source.InvokeCommand(fixture);

        executionCount.Should().Be(1);
    }

    /// <summary>
    /// Test that determines whether [is executing is behavioral].
    /// </summary>
    [Fact]
    public void IsExecutingIsBehavioral()
    {
        var fixture = RxCommand.Create(
            () => Observable.Return(Unit.Default),
            outputScheduler: ImmediateScheduler.Instance);
        fixture.IsExecuting.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var isExecuting).Subscribe();

        isExecuting.Should().ContainSingle(x => !x);
    }

    /// <summary>
    /// Test that determines whether [is executing remains true as long as execution pipeline has not completed].
    /// </summary>
    [Fact]
    public void IsExecutingRemainsTrueAsLongAsExecutionPipelineHasNotCompleted()
    {
        var execute = new Subject<Unit>();
        var fixture = RxCommand.Create<Unit>(() => execute, outputScheduler: ImmediateScheduler.Instance);

        fixture.Execute().Subscribe();

        Assert.True(fixture.IsExecuting.FirstAsync().Wait());

        execute.OnNext(Unit.Default);
        Assert.True(fixture.IsExecuting.FirstAsync().Wait());

        execute.OnNext(Unit.Default);
        Assert.True(fixture.IsExecuting.FirstAsync().Wait());

        execute.OnCompleted();
        Assert.False(fixture.IsExecuting.FirstAsync().Wait());
    }

    /// <summary>
    /// Test that determines whether [is executing ticks as executions progress].
    /// </summary>
    [Fact]
    public void IsExecutingTicksAsExecutionsProgress() =>
        new TestScheduler().With(
            scheduler =>
            {
                var execute = Observable.Return(Unit.Default).Delay(TimeSpan.FromSeconds(1), scheduler);
                var fixture = RxCommand.Create(() => execute, outputScheduler: scheduler);
                fixture.IsExecuting.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var isExecuting)
                    .Subscribe();

                fixture.Execute().Subscribe();
                scheduler.AdvanceByMs(100);

                isExecuting.Should().HaveCount(2);
                isExecuting.Should().SatisfyRespectively(
                    first => first.Should().BeFalse(),
                    second => second.Should().BeTrue());

                scheduler.AdvanceByMs(901);

                isExecuting.Should().HaveCount(3);
                Assert.False(isExecuting[2]);
            });

    /// <summary>
    /// Results the is ticked through specified scheduler.
    /// </summary>
    [Fact]
    public void ResultIsTickedThroughSpecifiedScheduler() =>
        new TestScheduler().With(
            scheduler =>
            {
                var fixture = RxCommand.Create(() => Observable.Return(Unit.Default), outputScheduler: scheduler);
                fixture.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var results).Subscribe();

                fixture.Execute().Subscribe();
                results.Should().BeEmpty();

                scheduler.AdvanceByMs(1);
                results.Should().HaveCount(1);
            });

    /// <summary>
    /// Synchronizes the command execute lazily.
    /// </summary>
    [Fact]
    public void SynchronousCommandExecuteLazily()
    {
        var executionCount = 0;
        var fixture1 = RxCommand.Create(() => ++executionCount, outputScheduler: ImmediateScheduler.Instance);
        var fixture2 = RxCommand.Create<int>(_ => ++executionCount, outputScheduler: ImmediateScheduler.Instance);
        var fixture3 = RxCommand.Create(
            () =>
            {
                ++executionCount;
                return 42;
            },
            outputScheduler: ImmediateScheduler.Instance);
        var fixture4 = RxCommand.Create<int, int>(
            _ =>
            {
                ++executionCount;
                return 42;
            },
            outputScheduler: ImmediateScheduler.Instance);
        var execute1 = fixture1.Execute();
        var execute2 = fixture2.Execute();
        var execute3 = fixture3.Execute();
        var execute4 = fixture4.Execute();

        executionCount.Should().Be(0);

        execute1.Subscribe();
        executionCount.Should().Be(1);

        execute2.Subscribe();
        executionCount.Should().Be(2);

        execute3.Subscribe();
        executionCount.Should().Be(3);

        execute4.Subscribe();
        executionCount.Should().Be(4);
    }

    /// <summary>
    /// Synchronizes the commands fail correctly.
    /// </summary>
    [Fact]
    public void SynchronousCommandsFailCorrectly()
    {
        var fixture1 = RxCommand.Create(
            () => throw new InvalidOperationException(),
            outputScheduler: ImmediateScheduler.Instance);
        var fixture2 = RxCommand.Create<int>(
            _ => throw new InvalidOperationException(),
            outputScheduler: ImmediateScheduler.Instance);
        var fixture3 = RxCommand.Create(
            () => throw new InvalidOperationException(),
            outputScheduler: ImmediateScheduler.Instance);
        Func<int, int> execute = _ => throw new InvalidOperationException();
        var fixture4 = RxCommand.Create(
            execute,
            outputScheduler: ImmediateScheduler.Instance);

        var failureCount = 0;
        Observable.Merge(
            fixture1.ThrownExceptions,
            fixture2.ThrownExceptions,
            fixture3.ThrownExceptions,
            fixture4.ThrownExceptions)
            .Subscribe(_ => ++failureCount);

        fixture1.Execute().Subscribe(_ => { }, _ => { });

        failureCount.Should().Be(1);

        fixture2.Execute().Subscribe(_ => { }, _ => { });
        failureCount.Should().Be(2);

        fixture3.Execute().Subscribe(_ => { }, _ => { });
        failureCount.Should().Be(3);

        fixture4.Execute().Subscribe(_ => { }, _ => { });
        failureCount.Should().Be(4);
    }

    /// <summary>
    /// Tests RxCommand from an invoke command.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RxCommandCreateHandlesTaskExceptionAsync()
    {
        var subj = new Subject<Unit>();
        var isExecuting = false;
        Exception? fail = null;

        async Task Execute()
        {
            await subj.Take(1);
            throw new Exception("break execution");
        }

        var fixture = RxCommand.Create(Execute, outputScheduler: ImmediateScheduler.Instance);

        fixture.IsExecuting.Subscribe(x => isExecuting = x);
        fixture.ThrownExceptions.Subscribe(ex => fail = ex);

        isExecuting.Should().BeFalse();
        fail.Should().BeNull();

        fixture.Execute().Subscribe();
        isExecuting.Should().BeTrue();
        fail.Should().BeNull();

        subj.OnNext(Unit.Default);

        // Wait 10 ms to allow execution to complete
        await Task.Delay(10).ConfigureAwait(false);

        isExecuting.Should().BeFalse();
        fail?.Message.Should().NotBeNull().And.Subject.Should().Be("break execution");
    }

    /// <summary>
    /// Tests RxCommand from an invoke command.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RxCommandExecutesFromInvokeCommand()
    {
        var semaphore = new SemaphoreSlim(0);
        var command = RxCommand.Create(() => semaphore.Release());

        Observable.Return(Unit.Default)
            .InvokeCommand(command);

        var result = 0;
        var task = semaphore.WaitAsync();
        if (await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(100)), task).ConfigureAwait(true) == task)
        {
            result = 1;
        }
        else
        {
            result = -1;
        }

        await Task.Delay(200).ConfigureAwait(false);
        result.Should().Be(1);
    }

    /// <summary>
    /// Tests RxCommand can dispose self.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RxCommandCanDisposeSelf()
    {
        var command = RxCommand.Create(() => new object());
        var waiter = WaitForValueAsync();
        command.Dispose();
        Assert.Null(await waiter.ConfigureAwait(false));

        async Task<object?> WaitForValueAsync() => await command.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Tests RxCommand can dispose self.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RxCommandCanDisposeSelfAfterSubscribe()
    {
        var command = RxCommand.Create(() => "RxCommand", outputScheduler: ImmediateScheduler.Instance);
        var disposables = new CompositeDisposable
        {
            command
        };
        var waiter = WaitForValueAsync();
        var valueRecieved = false;
        string? executeStringRecieved = null;
        string? stringRecieved = null;
        disposables.Add(command.Subscribe(
            s =>
            {
                stringRecieved = s;
                valueRecieved = true;
            },
            () => valueRecieved = true));
        disposables.Add(command.Execute().Subscribe(s => executeStringRecieved = s));
        Assert.True(valueRecieved);
        Assert.Equal("RxCommand", executeStringRecieved);
        Assert.Equal("RxCommand", stringRecieved);
        disposables.Dispose();
        Assert.Equal("RxCommand", await waiter.ConfigureAwait(false));

        async Task<string?> WaitForValueAsync() => await command.FirstOrDefaultAsync();
    }
}
