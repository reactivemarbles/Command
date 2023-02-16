// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive.Linq;

namespace ReactiveMarbles.Command;

internal static class ObservableConstants
{
    internal static readonly IObservable<bool> True = Observable.Return(true);
    internal static readonly IObservable<bool> False = Observable.Return(false);
}

internal static class ObservableConstants<TResult>
{
    internal static readonly IObservable<TResult> Empty = Observable.Empty<TResult>();
}
