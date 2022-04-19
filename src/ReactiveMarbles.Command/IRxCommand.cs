// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Windows.Input;

namespace ReactiveMarbles.Command;

/// <summary>
/// Encapsulates a user action behind a reactive interface.
/// This is for interop inside for the command binding.
/// Not meant for external use due to the fact it doesn't implement ICommand
/// to force the user to favor the Reactive style command execution.
/// </summary>
public interface IRxCommand : ICommand, IDisposable
{
    /// <summary>
    /// Gets an observable whose value indicates whether the command is currently executing.
    /// </summary>
    /// <remarks>
    /// This observable can be particularly useful for updating UI, such as showing an activity indicator whilst a command
    /// is executing.
    /// </remarks>
    IObservable<bool> IsExecuting { get; }

    /// <summary>
    /// Gets an observable whose value indicates whether the command can currently execute.
    /// </summary>
    /// <remarks>
    /// The value provided by this observable is governed both by any <c>canExecute</c> observable provided during
    /// command creation, as well as the current execution status of the command. A command that is currently executing
    /// will always yield <c>false</c> from this observable, even if the <c>canExecute</c> pipeline is currently <c>true</c>.
    /// </remarks>
    IObservable<bool> CanCommandExecute { get; }

    /// <summary>
    /// Gets an observable that ticks any exceptions in command execution logic.
    /// </summary>
    /// <remarks>
    /// Any exceptions that are not observed via this observable will propagate out and cause the application to be torn
    /// down. Therefore, you will always want to subscribe to this observable if you expect errors could occur (e.g. if
    /// your command execution includes network activity).
    /// </remarks>
    IObservable<Exception> ThrownExceptions { get; }
}

/// <summary>
/// Encapsulates a user action behind a reactive interface.
/// This is for interop inside for the command binding.
/// Not meant for external use due to the fact it doesn't implement ICommand
/// to force the user to favor the Reactive style command execution.
/// </summary>
/// <typeparam name="TParam">The parameter type passed to the command.</typeparam>
/// <typeparam name="TResult">The result type from the command.</typeparam>
public interface IRxCommand<in TParam, out TResult> : IRxCommand, IObservable<TResult>
{
    /// <summary>
    /// Gets an observable that, when subscribed, executes this command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Invoking this method will return a cold (lazy) observable that, when subscribed, will execute the logic
    /// encapsulated by the command. It is worth restating that the returned observable is lazy. Nothing will
    /// happen if you call <c>Execute</c> and neglect to subscribe (directly or indirectly) to the returned observable.
    /// </para>
    /// <para>
    /// If no parameter value is provided, a default value of type <typeparamref name="TParam"/> will be passed into
    /// the execution logic.
    /// </para>
    /// <para>
    /// Any number of subscribers can subscribe to a given execution observable and the execution logic will only
    /// run once. That is, the result is broadcast to those subscribers.
    /// </para>
    /// <para>
    /// In those cases where execution fails, there will be no result value. Instead, the failure will tick through the
    /// <see cref="IRxCommand.ThrownExceptions"/> observable.
    /// </para>
    /// </remarks>
    /// <returns>
    /// An observable that will tick the single result value if and when it becomes available.
    /// </returns>
    IObservable<TResult> Execute();

    /// <summary>
    /// Gets an observable that, when subscribed, executes this command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Invoking this method will return a cold (lazy) observable that, when subscribed, will execute the logic
    /// encapsulated by the command. It is worth restating that the returned observable is lazy. Nothing will
    /// happen if you call <c>Execute</c> and neglect to subscribe (directly or indirectly) to the returned observable.
    /// </para>
    /// <para>
    /// If no parameter value is provided, a default value of type <typeparamref name="TParam"/> will be passed into
    /// the execution logic.
    /// </para>
    /// <para>
    /// Any number of subscribers can subscribe to a given execution observable and the execution logic will only
    /// run once. That is, the result is broadcast to those subscribers.
    /// </para>
    /// <para>
    /// In those cases where execution fails, there will be no result value. Instead, the failure will tick through the
    /// <see cref="IRxCommand.ThrownExceptions"/> observable.
    /// </para>
    /// </remarks>
    /// <param name="parameter">
    /// The parameter to pass into command execution.
    /// </param>
    /// <returns>
    /// An observable that will tick the single result value if and when it becomes available.
    /// </returns>
    IObservable<TResult> Execute(TParam parameter);
}
