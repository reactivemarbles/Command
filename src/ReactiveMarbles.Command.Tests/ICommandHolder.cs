// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Windows.Input;
using ReactiveMarbles.Mvvm;

namespace ReactiveMarbles.Command.Tests;

/// <summary>
/// A ReactiveObject which hosts a command.
/// </summary>
public class ICommandHolder : RxObject
{
    private ICommand? _theCommand;

    /// <summary>
    /// Gets or sets the command.
    /// </summary>
    public ICommand? TheCommand
    {
        get => _theCommand;
        set => RaiseAndSetIfChanged(ref _theCommand, value);
    }
}
