﻿using Fluxor;
using Luthetus.Common.RazorLib.Keys.Models;
using Luthetus.Ide.RazorLib.Terminals.Models;
using System.Collections.Immutable;

namespace Luthetus.Ide.RazorLib.Terminals.States;

[FeatureState]
public partial record TerminalState(ImmutableDictionary<Key<Terminal>, Terminal> TerminalMap)
{
    public TerminalState()
        : this(ImmutableDictionary<Key<Terminal>, Terminal>.Empty)
    {
    }
}