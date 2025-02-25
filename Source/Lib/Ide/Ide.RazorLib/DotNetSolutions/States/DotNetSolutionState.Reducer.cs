﻿using Fluxor;
using Luthetus.Ide.RazorLib.DotNetSolutions.Models;

namespace Luthetus.Ide.RazorLib.DotNetSolutions.States;

public partial record DotNetSolutionState
{
    public class Reducer
    {
        [ReducerMethod]
        public static DotNetSolutionState ReduceRegisterAction(
            DotNetSolutionState inState,
            RegisterAction registerAction)
        {
            var dotNetSolutionModel = inState.DotNetSolutionModel;

            if (dotNetSolutionModel is not null)
                return inState;

            var nextList = inState.DotNetSolutionsList.Add(
                registerAction.DotNetSolutionModel);

            return inState with
            {
                DotNetSolutionsList = nextList
            };
        }

        [ReducerMethod]
        public static DotNetSolutionState ReduceDisposeAction(
            DotNetSolutionState inState,
            DisposeAction disposeAction)
        {
            var dotNetSolutionModel = inState.DotNetSolutionModel;

            if (dotNetSolutionModel is null)
                return inState;

            var nextList = inState.DotNetSolutionsList.Remove(
                dotNetSolutionModel);

            return inState with
            {
                DotNetSolutionsList = nextList
            };
        }

        [ReducerMethod]
        public static DotNetSolutionState ReduceWithAction(
            DotNetSolutionState inState,
            LuthetusIdeDotNetSolutionBackgroundTaskApi.IWithAction withActionInterface)
        {
            var withAction = (LuthetusIdeDotNetSolutionBackgroundTaskApi.WithAction)withActionInterface;
            return withAction.WithFunc.Invoke(inState);
        }
    }
}