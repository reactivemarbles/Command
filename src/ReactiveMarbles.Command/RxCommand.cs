// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveMarbles.Locator;
using ReactiveMarbles.Mvvm;

// ReSharper disable StaticMemberInGenericType
namespace ReactiveMarbles.Command;

/// <summary>
/// Encapsulates a user action behind a reactive interface.
/// </summary>
/// <remarks>
/// <para>
/// This non-generic base class defines the creation behavior of the RxCommand's.
/// </para>
/// <para>
/// <see cref="RxCommand{TInput, Output}"/> adds the concept of Input and Output generic types.
/// The Input is often passed in by the View and it's type is captured as TInput, and the Output is
/// the result of executing the command which type is captured as TOutput.
/// </para>
/// <para>
/// <see cref="RxCommand{TInput, Output}"/> is <c>IObservable</c> which can be used like any other <c>IObservable</c>.
/// For example, you can Subscribe() to it like any other observable, and add the output to a List on your view model.
/// The Unit type is a functional programming construct analogous to void and can be used in cases where you don't
/// care about either the input and/or output value.
/// </para>
/// <para>
/// Creating synchronous reactive commands:
/// <code>
/// <![CDATA[
/// // A synchronous command taking a parameter and returning nothing.
/// RxCommand<int, Unit> command = RxCommand.Create<int>(x => Console.WriteLine(x));
///
/// // This outputs 42 to console.
/// command.Execute(42).Subscribe();
///
/// // A better approach is to invoke a command in response to an Observable<T>.
/// // InvokeCommand operator respects the command's executability. That is, if
/// // the command's CanExecute method returns false, InvokeCommand will not
/// // execute the command when the source observable ticks.
/// Observable.Return(42).InvokeCommand(command);
/// ]]>
/// </code>
/// </para>
/// <para>
/// Creating asynchronous reactive commands:
/// <code>
/// <![CDATA[
/// // An asynchronous command that waits 2 seconds and returns 42.
/// var command = RxCommand.Create<Unit, int>(
///      _ => Observable.Return(42).Delay(TimeSpan.FromSeconds(2))
/// );
///
/// // Calling the asynchronous reactive command:
/// // Observable.Return(Unit.Default).InvokeCommand(command);
///
/// // Subscribing to values emitted by the command:
/// command.Subscribe(Console.WriteLine);
/// ]]>
/// </code>
/// </para>
/// </remarks>
public static class RxCommand
{
    /// <summary>
    /// Creates a parameterless <see cref="RxCommand{TParam, TResult}"/> with synchronous execution logic.
    /// </summary>
    /// <param name="execute">
    /// The action to execute whenever the command is executed.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    public static RxCommand<Unit, Unit> Create(
        Action execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null) =>
        new(_ => Observable.Create<Unit>(
            observer =>
            {
                execute();
                observer.OnNext(Unit.Default);
                observer.OnCompleted();
                return Disposable.Empty;
            }), canExecute, outputScheduler);

    /// <summary>
    /// Creates a <see cref="RxCommand{TParam, TResult}"/> with synchronous execution logic that takes a parameter of type <typeparamref name="TParam"/>
    /// and returns a value of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <param name="execute">
    /// The function to execute whenever the command is executed.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    /// <typeparam name="TParam">
    /// The type of the parameter passed through to command execution.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type of value returned by command executions.
    /// </typeparam>
    public static RxCommand<TParam, TResult> Create<TParam, TResult>(
        Func<TParam, TResult> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null) =>
        new(execute, canExecute, outputScheduler);

    /// <summary>
    /// Creates a <see cref="RxCommand{TParam, TResult}"/> with synchronous execution logic that takes a parameter of type <typeparamref name="TParam"/>.
    /// </summary>
    /// <param name="execute">
    /// The action to execute whenever the command is executed.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    /// <typeparam name="TParam">
    /// The type of the parameter passed through to command execution.
    /// </typeparam>
    public static RxCommand<TParam, Unit> Create<TParam>(
        Action<TParam> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute == null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new(param =>
        {
            execute(param);
            return Unit.Default;
        }, canExecute, outputScheduler);
    }

    /// <summary>
    /// Creates a parameterless <see cref="RxCommand{TParam, TResult}"/> with synchronous execution logic that returns a value
    /// of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <param name="execute">
    /// The function to execute whenever the command is executed.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    /// <typeparam name="TResult">
    /// The type of value returned by command executions.
    /// </typeparam>
    public static RxCommand<Unit, TResult> Create<TResult>(
        Func<TResult> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute == null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new(_ => execute(), canExecute, outputScheduler);
    }

    /// <summary>
    /// Creates a parameterless <see cref="RxCommand{TParam, TResult}"/> with asynchronous execution logic.
    /// </summary>
    /// <param name="execute">
    /// Provides an observable representing the command's asynchronous execution logic.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    /// <typeparam name="TResult">
    /// The type of the command's result.
    /// </typeparam>
    public static RxCommand<Unit, TResult> Create<TResult>(
        Func<IObservable<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute is null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new RxCommand<Unit, TResult>(
            _ => execute(),
            canExecute ?? ObservableConstants.True,
            outputScheduler);
    }

    /// <summary>
    /// Creates a <see cref="RxCommand{TParam, TResult}"/> with asynchronous execution logic that takes a parameter of type <typeparamref name="TParam"/>.
    /// </summary>
    /// <param name="execute">
    /// Provides an observable representing the command's asynchronous execution logic.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    /// <typeparam name="TParam">
    /// The type of the parameter passed through to command execution.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type of the command's result.
    /// </typeparam>
    public static RxCommand<TParam, TResult> Create<TParam, TResult>(
        Func<TParam, IObservable<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute is null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new RxCommand<TParam, TResult>(
            execute,
            canExecute ?? ObservableConstants.True,
            outputScheduler);
    }

    /// <summary>
    /// Creates a parameterless <see cref="RxCommand{TParam, TResult}"/> with asynchronous execution logic.
    /// </summary>
    /// <param name="execute">
    /// Provides a <see cref="Task"/> representing the command's asynchronous execution logic.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    /// <typeparam name="TResult">
    /// The type of the command's result.
    /// </typeparam>
    public static RxCommand<Unit, TResult> Create<TResult>(
        Func<Task<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute is null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new RxCommand<Unit, TResult>(_ => execute(), canExecute, outputScheduler);
    }

    /// <summary>
    /// Creates a parameterless <see cref="RxCommand{TParam, TResult}"/> with asynchronous execution logic.
    /// </summary>
    /// <param name="execute">
    /// Provides a <see cref="Task"/> representing the command's asynchronous execution logic.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    public static RxCommand<Unit, Unit> Create(
        Func<Task> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute is null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new RxCommand<Unit, Unit>(
            async _ =>
            {
                await execute().ConfigureAwait(false);
                return Unit.Default;
            },
            canExecute,
            outputScheduler);
    }

    /// <summary>
    /// Creates a parameterless, cancellable <see cref="RxCommand{TParam, TResult}"/> with asynchronous execution logic.
    /// </summary>
    /// <param name="execute">
    /// Provides a <see cref="Task"/> representing the command's asynchronous execution logic.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    public static RxCommand<Unit, Unit> Create(
        Func<CancellationToken, Task> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute is null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new RxCommand<Unit, Unit>(
            async (_, ct) =>
            {
                await execute(ct).ConfigureAwait(false);
                return Unit.Default;
            },
            canExecute,
            outputScheduler);
    }

    /// <summary>
    /// Creates a <see cref="RxCommand{TParam, TResult}"/> with asynchronous execution logic that takes a parameter of type <typeparamref name="TParam"/>.
    /// </summary>
    /// <param name="execute">
    /// Provides a <see cref="Task"/> representing the command's asynchronous execution logic.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    /// <typeparam name="TParam">
    /// The type of the parameter passed through to command execution.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type of the command's result.
    /// </typeparam>
    public static RxCommand<TParam, TResult> Create<TParam, TResult>(
        Func<TParam, Task<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute is null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new RxCommand<TParam, TResult>(
            execute,
            canExecute,
            outputScheduler);
    }

    /// <summary>
    /// Creates a <see cref="RxCommand{TParam, TResult}"/> with asynchronous, cancellable execution logic that takes a parameter of type <typeparamref name="TParam"/>.
    /// </summary>
    /// <param name="execute">
    /// Provides a <see cref="Task"/> representing the command's asynchronous execution logic.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    /// <typeparam name="TParam">
    /// The type of the parameter passed through to command execution.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type of the command's result.
    /// </typeparam>
    public static RxCommand<TParam, TResult> Create<TParam, TResult>(
        Func<TParam, CancellationToken, Task<TResult>> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute is null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new RxCommand<TParam, TResult>(
            execute,
            canExecute,
            outputScheduler);
    }

    /// <summary>
    /// Creates a <see cref="RxCommand{TParam, TResult}"/> with asynchronous execution logic that takes a parameter of type <typeparamref name="TParam"/>.
    /// </summary>
    /// <param name="execute">
    /// Provides a <see cref="Task"/> representing the command's asynchronous execution logic.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    /// <typeparam name="TParam">
    /// The type of the parameter passed through to command execution.
    /// </typeparam>
    public static RxCommand<TParam, Unit> Create<TParam>(
        Func<TParam, Task> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute is null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new RxCommand<TParam, Unit>(
            param => execute(param).ToObservable(),
            canExecute,
            outputScheduler);
    }

    /// <summary>
    /// Creates a <see cref="RxCommand{TParam, TResult}"/> with asynchronous, cancellable execution logic that takes a parameter of type <typeparamref name="TParam"/>.
    /// </summary>
    /// <param name="execute">
    /// Provides a <see cref="Task"/> representing the command's asynchronous execution logic.
    /// </param>
    /// <param name="canExecute">
    /// An optional observable that dictates the availability of the command for execution.
    /// </param>
    /// <param name="outputScheduler">
    /// An optional scheduler that is used to surface events. Defaults to <c>RxApp.MainThreadScheduler</c>.
    /// </param>
    /// <returns>
    /// The <c>RxCommand</c> instance.
    /// </returns>
    /// <typeparam name="TParam">
    /// The type of the parameter passed through to command execution.
    /// </typeparam>
    public static RxCommand<TParam, Unit> Create<TParam>(
        Func<TParam, CancellationToken, Task> execute,
        IObservable<bool>? canExecute = null,
        IScheduler? outputScheduler = null)
    {
        if (execute is null)
        {
            throw new ArgumentNullException(nameof(execute));
        }

        return new RxCommand<TParam, Unit>(
            async (param, ct) =>
            {
                await execute(param, ct).ConfigureAwait(false);
                return Unit.Default;
            },
            canExecute,
            outputScheduler);
    }
}

/// <summary>
/// Encapsulates a user interaction behind a reactive interface.
/// </summary>
/// <typeparam name="TParam">
/// The type of parameter values passed in during command execution.
/// </typeparam>
/// <typeparam name="TResult">
/// The type of the values that are the result of command execution.
/// </typeparam>
public class RxCommand<TParam, TResult> : IRxCommand<TParam, TResult>
{
    private readonly IObservable<bool> _canExecute;
    private readonly IDisposable _canExecuteSubscription;
    private readonly ScheduledSubject<Exception> _exceptions;
    private readonly Func<TParam, IObservable<TResult>> _execute;
    private readonly Subject<ExecutionInfo> _executionInfo;
    private readonly IObservable<bool> _isExecuting;
    private readonly IObservable<TResult> _results;
    private readonly ISubject<ExecutionInfo, ExecutionInfo> _synchronizedExecutionInfo;
    private EventHandler? _canExecuteChanged;
    private bool _canExecuteValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="RxCommand{TParam, TResult}"/> class.
    /// </summary>
    /// <param name="execute">The Func to perform when the command is executed.</param>
    /// <param name="canExecute">A observable which has a value if the command can execute.</param>
    /// <param name="outputScheduler">The scheduler where to send output after the main execution.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependent parameters are null.</exception>
    public RxCommand(
        Func<TParam, TResult> execute,
        IObservable<bool>? canExecute,
        IScheduler? outputScheduler = null)
        : this(
            param => Observable.Create<TResult>(observer =>
            {
                observer.OnNext(execute(param));
                observer.OnCompleted();
                return Disposable.Empty;
            }),
            canExecute,
            outputScheduler)
    {
        if (execute == null)
        {
            throw new ArgumentNullException(nameof(execute));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RxCommand{TParam, TResult}"/> class.
    /// </summary>
    /// <param name="execute">The Func to perform when the command is executed.</param>
    /// <param name="canExecute">A observable which has a value if the command can execute.</param>
    /// <param name="outputScheduler">The scheduler where to send output after the main execution.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependent parameters are null.</exception>
    public RxCommand(
        Func<TParam, Task<TResult>> execute,
        IObservable<bool>? canExecute,
        IScheduler? outputScheduler = null)
        : this(param => Observable.FromAsync(() => execute(param)), canExecute, outputScheduler)
    {
        if (execute == null)
        {
            throw new ArgumentNullException(nameof(execute));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RxCommand{TParam, TResult}"/> class.
    /// </summary>
    /// <param name="execute">The Func to perform when the command is executed.</param>
    /// <param name="canExecute">A observable which has a value if the command can execute.</param>
    /// <param name="outputScheduler">The scheduler where to send output after the main execution.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependent parameters are null.</exception>
    public RxCommand(
        Func<TParam, CancellationToken, Task<TResult>> execute,
        IObservable<bool>? canExecute,
        IScheduler? outputScheduler = null)
        : this(param => Observable.FromAsync(ct => execute(param, ct)), canExecute, outputScheduler)
    {
        if (execute == null)
        {
            throw new ArgumentNullException(nameof(execute));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RxCommand{TParam, TResult}"/> class.
    /// </summary>
    /// <param name="execute">The Func to perform when the command is executed.</param>
    /// <param name="canExecute">A observable which has a value if the command can execute.</param>
    /// <param name="outputScheduler">The scheduler where to send output after the main execution.</param>
    /// <exception cref="ArgumentNullException">Thrown if any dependent parameters are null.</exception>
    public RxCommand(
        Func<TParam, IObservable<TResult>> execute,
        IObservable<bool>? canExecute,
        IScheduler? outputScheduler = null)
    {
        canExecute ??= ObservableConstants.True;

        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        var coreRegistration = ServiceLocator.Current().GetService<ICoreRegistration>();
        var scheduler = outputScheduler ?? coreRegistration.MainThreadScheduler;
        _exceptions = new ScheduledSubject<Exception>(scheduler, coreRegistration.ExceptionHandler);
        _executionInfo = new Subject<ExecutionInfo>();
        _synchronizedExecutionInfo = Subject.Synchronize(_executionInfo, scheduler);
        _isExecuting = _synchronizedExecutionInfo.Scan(
                0,
                (acc, next) =>
                {
                    return next.Demarcation switch
                    {
                        ExecutionDemarcation.Begin => acc + 1,
                        ExecutionDemarcation.End => acc - 1,
                        _ => acc
                    };
                }).Select(inFlightCount => inFlightCount > 0)
            .StartWith(false)
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();

        _canExecute = canExecute.Catch<bool, Exception>(
                ex =>
                {
                    _exceptions.OnNext(ex);
                    return ObservableConstants.False;
                }).StartWith(false)
            .CombineLatest(_isExecuting, (canEx, isEx) => canEx && !isEx)
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();

        _results = _synchronizedExecutionInfo.Where(x => x.Demarcation == ExecutionDemarcation.Result)
            .Select(x => x.Result);

        _canExecuteSubscription = _canExecute
            .Subscribe(OnCanExecuteChanged);
    }

    /// <inheritdoc/>
    event EventHandler? ICommand.CanExecuteChanged
    {
        add => _canExecuteChanged += value;
        remove => _canExecuteChanged -= value;
    }

    private enum ExecutionDemarcation
    {
        Begin,

        Result,

        End
    }

    /// <inheritdoc/>
    public IObservable<bool> CanCommandExecute => _canExecute;

    /// <inheritdoc/>
    public IObservable<bool> IsExecuting => _isExecuting;

    /// <inheritdoc/>
    public IObservable<Exception> ThrownExceptions => _exceptions.AsObservable();

    /// <inheritdoc/>
    bool ICommand.CanExecute(object? parameter) => ICommandCanExecute(parameter);

    /// <inheritdoc/>
    void ICommand.Execute(object? parameter) => ICommandExecute(parameter);

    /// <inheritdoc cref="IRxCommand{TParam,TResult}" />
    public IObservable<TResult> Execute(TParam parameter)
    {
        try
        {
            return Observable.Defer(
                    () =>
                    {
                        _synchronizedExecutionInfo.OnNext(ExecutionInfo.CreateBegin());
                        return ObservableConstants<TResult>.Empty;
                    }).Concat(_execute(parameter))
                .Do(result => _synchronizedExecutionInfo.OnNext(ExecutionInfo.CreateResult(result)))
                .Catch<TResult, Exception>(
                    ex =>
                    {
                        _synchronizedExecutionInfo.OnNext(ExecutionInfo.CreateEnd());
                        _exceptions.OnNext(ex);
                        return Observable.Throw<TResult>(ex);
                    }).Finally(() => _synchronizedExecutionInfo.OnNext(ExecutionInfo.CreateEnd()))
                .PublishLast()
                .RefCount();
        }
        catch (Exception ex)
        {
            _synchronizedExecutionInfo.OnNext(ExecutionInfo.CreateEnd());
            _exceptions.OnNext(ex);
            return Observable.Throw<TResult>(ex);
        }
    }

    /// <inheritdoc cref="IRxCommand{TParam,TResult}" />
    public IObservable<TResult> Execute()
    {
        try
        {
            return Observable.Defer(
                    () =>
                    {
                        _synchronizedExecutionInfo.OnNext(ExecutionInfo.CreateBegin());
                        return ObservableConstants<TResult>.Empty;
                    }).Concat(_execute(default!))
                .Do(result => _synchronizedExecutionInfo.OnNext(ExecutionInfo.CreateResult(result)))
                .Catch<TResult, Exception>(
                    ex =>
                    {
                        _synchronizedExecutionInfo.OnNext(ExecutionInfo.CreateEnd());
                        _exceptions.OnNext(ex);
                        return Observable.Throw<TResult>(ex);
                    }).Finally(() => _synchronizedExecutionInfo.OnNext(ExecutionInfo.CreateEnd()))
                .PublishLast()
                .RefCount();
        }
        catch (Exception ex)
        {
            _synchronizedExecutionInfo.OnNext(ExecutionInfo.CreateEnd());
            _exceptions.OnNext(ex);
            return Observable.Throw<TResult>(ex);
        }
    }

    /// <inheritdoc cref="IRxCommand" />
    public IDisposable Subscribe(IObserver<TResult> observer) => _results.Subscribe(observer);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Will trigger a event when the CanExecute condition has changed.
    /// </summary>
    /// <param name="newValue">The new value of the execute.</param>
    protected virtual void OnCanExecuteChanged(bool newValue)
    {
        _canExecuteValue = newValue;
        _canExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Dispose of the members that are disposable.
    /// </summary>
    /// <param name="disposing">A value that indicates whether or not is is being disposed by the dispose method.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _executionInfo.Dispose();
        _exceptions.Dispose();
        _canExecuteSubscription.Dispose();
    }

    /// <summary>
    /// Will be called by the methods from the ICommand interface.
    /// This method is called when the Command should evaluate if it can execute.
    /// </summary>
    /// <param name="parameter">The parameter being passed to the ICommand.</param>
    /// <returns>If the command can be executed.</returns>
    protected virtual bool ICommandCanExecute(object? parameter) => _canExecuteValue;

    /// <summary>
    /// Will be called by the methods from the ICommand interface.
    /// This method is called when the Command should execute.
    /// </summary>
    /// <param name="parameter">The parameter being passed to the ICommand.</param>
    protected virtual void ICommandExecute(object? parameter)
    {
        // ensure that null is coerced to default(TParam) so that commands taking value types will use a sensible default if no parameter is supplied
        parameter ??= default(TParam);

        if (parameter is not null && !(parameter is TParam))
        {
            throw new InvalidOperationException(
                $"Command requires parameters of type {typeof(TParam).FullName}, but received parameter of type {parameter.GetType().FullName}.");
        }

        var result = parameter is null ? Execute() : Execute((TParam)parameter);

        result
            .Catch(ObservableConstants<TResult>.Empty)
            .Subscribe();
    }

    private readonly struct ExecutionInfo
    {
        private ExecutionInfo(ExecutionDemarcation demarcation, TResult result)
        {
            Demarcation = demarcation;
            Result = result;
        }

        public ExecutionDemarcation Demarcation { get; }

        public TResult Result { get; }

        public static ExecutionInfo CreateBegin() => new(ExecutionDemarcation.Begin, default!);

        public static ExecutionInfo CreateResult(TResult result) =>
            new(ExecutionDemarcation.Result, result);

        public static ExecutionInfo CreateEnd() => new(ExecutionDemarcation.End, default!);
    }
}
