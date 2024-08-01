﻿using PixiEditor.Models.Commands.Commands;

namespace PixiEditor.Models.Commands.Evaluators;

internal class CanExecuteEvaluator : Evaluator<bool>
{
    public static CanExecuteEvaluator AlwaysTrue { get; } = new StaticValueEvaluator(true);

    public static CanExecuteEvaluator AlwaysFalse { get; } = new StaticValueEvaluator(false);
    public string[]? DependentOn { get; set; } // TODO: It is used in CanExecuteChanged event, but it's commented out because it might not impact performance

    private class StaticValueEvaluator : CanExecuteEvaluator
    {
        private readonly bool value;

        public StaticValueEvaluator(bool value)
        {
            this.value = value;
        }

        public override bool CallEvaluate(Command command, object parameter) => value;
    }
}
