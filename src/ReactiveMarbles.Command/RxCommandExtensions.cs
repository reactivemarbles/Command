// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveMarbles.PropertyChanged;

namespace ReactiveMarbles.Command;

/// <summary>
/// Extension method for invoking commands.
/// </summary>
public static class RxCommandExtensions
{
    /// <summary>
    /// A utility method that will pipe an Observable to an ICommand (i.e.
    /// it will first call its CanExecute with the provided value, then if
    /// the command can be executed, Execute() will be called).
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <param name="item">The source observable to pipe into the command.</param>
    /// <param name="command">The command to be executed.</param>
    /// <returns>An object that when disposes, disconnects the Observable
    /// from the command.</returns>
    public static IDisposable InvokeCommand<T>(this IObservable<T> item, ICommand? command)
    {
        var canExecuteChanged = Observable.FromEvent<EventHandler, Unit>(
                eventHandler =>
                {
                    void Handler(object? sender, EventArgs e) => eventHandler(Unit.Default);
                    return Handler;
                },
                h => command!.CanExecuteChanged += h,
                h => command!.CanExecuteChanged -= h)
            .StartWith(Unit.Default);

        return WithLatestFromFixed(item, canExecuteChanged, (value, _) => new InvokeCommandInfo<ICommand?, T>(command, command!.CanExecute(value), value))
            .Where(ii => ii.CanExecute)
            .Do(ii => command?.Execute(ii.Value))
            .Subscribe();
    }

    /// <summary>
    /// A utility method that will pipe an Observable to an ICommand (i.e.
    /// it will first call its CanExecute with the provided value, then if
    /// the command can be executed, Execute() will be called).
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="item">The source observable to pipe into the command.</param>
    /// <param name="command">The command to be executed.</param>
    /// <returns>An object that when disposes, disconnects the Observable
    /// from the command.</returns>
    public static IDisposable InvokeCommand<T, TResult>(this IObservable<T> item, RxCommand<T, TResult>? command) =>
        command is null
            ? throw new ArgumentNullException(nameof(command))
            : WithLatestFromFixed(item, command.CanCommandExecute, (value, canExecute) => new InvokeCommandInfo<RxCommand<T, TResult>, T>(command, canExecute, value))
                .Where(ii => ii.CanExecute)
                .SelectMany(ii => command.Execute(ii.Value).Catch(ObservableConstants<TResult>.Empty))
                .Subscribe();

    /// <summary>
    /// A utility method that will pipe an Observable to an ICommand (i.e.
    /// it will first call its CanExecute with the provided value, then if
    /// the command can be executed, Execute() will be called).
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="item">The source observable to pipe into the command.</param>
    /// <param name="target">The root object which has the Command.</param>
    /// <param name="commandProperty">The expression to reference the Command.</param>
    /// <returns>An object that when disposes, disconnects the Observable
    /// from the command.</returns>
    public static IDisposable InvokeCommand<T, TTarget>(this IObservable<T> item, TTarget? target, Expression<Func<TTarget, ICommand?>> commandProperty)
        where TTarget : class, INotifyPropertyChanged
    {
        if (commandProperty == null)
        {
            throw new ArgumentNullException(nameof(commandProperty));
        }

        var commandObs =
            target.WhenChanged(commandProperty!)
                .Where(x => x != null)
                .Select(x => x!);
        var commandCanExecuteChanged = commandObs
            .Select(command => command is null
                ? ObservableConstants<ICommand>.Empty
                : Observable
                    .FromEvent<EventHandler, ICommand>(
                        eventHandler => (_, _) => eventHandler(command),
                        h => command.CanExecuteChanged += h,
                        h => command.CanExecuteChanged -= h)
                    .StartWith(command))
            .Switch();

        return WithLatestFromFixed(
                item,
                commandCanExecuteChanged,
                (value, cmd) => new InvokeCommandInfo<ICommand, T>(cmd, cmd.CanExecute(value), value))
            .Where(ii => ii.CanExecute)
            .Do(ii => ii.Command.Execute(ii.Value))
            .Subscribe();
    }

    /// <summary>
    /// A utility method that will pipe an Observable to an ICommand (i.e.
    /// it will first call its CanExecute with the provided value, then if
    /// the command can be executed, Execute() will be called).
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <param name="item">The source observable to pipe into the command.</param>
    /// <param name="target">The root object which has the Command.</param>
    /// <param name="commandProperty">The expression to reference the Command.</param>
    /// <returns>An object that when disposes, disconnects the Observable
    /// from the command.</returns>
    public static IDisposable InvokeCommand<T, TResult, TTarget>(
        this IObservable<T> item,
        TTarget? target,
        Expression<Func<TTarget, RxCommand<T, TResult>?>> commandProperty)
        where TTarget : class, INotifyPropertyChanged
    {
        if (commandProperty == null)
        {
            throw new ArgumentNullException(nameof(commandProperty));
        }

        var command =
            target.WhenChanged(commandProperty!)
                .Where(x => x != null)
                .Select(x => x!);
        var invocationInfo = command
            .Select(cmd => cmd is null
                ? ObservableConstants<InvokeCommandInfo<RxCommand<T, TResult>, T>>.Empty
                : cmd
                    .CanCommandExecute
                    .Select(canExecute => new InvokeCommandInfo<RxCommand<T, TResult>, T>(cmd, canExecute)))
            .Switch();

        return WithLatestFromFixed(item, invocationInfo, (value, ii) => ii.WithValue(value))
            .Where(ii => ii.CanExecute)
            .SelectMany(ii => ii.Command.Execute(ii.Value).Catch(ObservableConstants<TResult>.Empty))
            .Subscribe();
    }

    /// <summary>
    /// A utility method that will pipe an IRxCommand from an Observable (i.e.
    /// it will first call its CanExecute with the provided value, then if
    /// the command can be executed, Execute() will be called).
    /// </summary>
    /// <typeparam name="TParam">The type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="source">The source observable to pipe into the command.</param>
    /// <returns>An object that when disposes, disconnects the Observable
    /// from the command.</returns>
    public static IDisposable InvokeCommand<TParam, TResult>(this IObservable<TParam> source)
        where TParam : IRxCommand<TParam, TResult>
    {
        var command = source.Where(x => x != null).Select(x => x!);
        var invocationInfo = command
            .Select(cmd => cmd is null
                ? ObservableConstants<InvokeCommandInfo<IRxCommand<TParam, TResult>, TParam>>.Empty
                : cmd
                    .CanCommandExecute
                    .Select(canExecute => new InvokeCommandInfo<IRxCommand<TParam, TResult>, TParam>(cmd, canExecute)))
            .Switch();

        return WithLatestFromFixed(source, invocationInfo, (value, ii) => ii.WithValue(value))
            .Where(ii => ii.CanExecute)
            .SelectMany(ii => ii.Command.Execute(ii.Value).Catch(ObservableConstants<TResult>.Empty))
            .Subscribe();
    }

    // See https://github.com/Reactive-Extensions/Rx.NET/issues/444
    private static IObservable<TResult> WithLatestFromFixed<TLeft, TRight, TResult>(
        IObservable<TLeft> item,
        IObservable<TRight> other,
        Func<TLeft, TRight, TResult> resultSelector) =>
        item
            .Publish(
                os =>
                    other
                        .Select(
                            a =>
                                os
                                    .Select(b => resultSelector(b, a)))
                        .Switch());

    private readonly struct InvokeCommandInfo<TCommand, TValue>
    {
        public InvokeCommandInfo(TCommand command, bool canExecute, TValue value)
        {
            Command = command;
            CanExecute = canExecute;
            Value = value!;
        }

        public InvokeCommandInfo(TCommand command, bool canExecute)
        {
            Command = command;
            CanExecute = canExecute;
            Value = default!;
        }

        public TCommand Command { get; }

        public bool CanExecute { get; }

        public TValue Value { get; }

        public InvokeCommandInfo<TCommand, TValue> WithValue(TValue value) =>
            new(Command, CanExecute, value);
    }
}
