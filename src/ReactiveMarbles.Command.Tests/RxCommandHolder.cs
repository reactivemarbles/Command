// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;
using ReactiveMarbles.Mvvm;

namespace ReactiveMarbles.Command.Tests;

/// <summary>
/// A ReactiveObject which hosts a ReactiveCommand.
/// </summary>
/// <seealso cref="ReactiveUI.ReactiveObject" />
public class RxCommandHolder : RxObject
{
    private RxCommand<int, Unit>? _theCommand;

    /// <summary>
    /// Gets or sets the command.
    /// </summary>
    public RxCommand<int, Unit>? TheCommand
    {
        get => _theCommand;
        set => RaiseAndSetIfChanged(ref _theCommand, value);
    }
}
