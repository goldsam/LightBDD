using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightBDD.Core.Extensibility;
using LightBDD.Core.Extensibility.Results;

namespace LightBDD.ScenarioHelpers
{
    public class TestCompositeStep : CompositeStepResultDescriptor
    {
        public TestCompositeStep(params StepDescriptor[] subSteps) : this(() => null, subSteps) { }
        public TestCompositeStep(Func<object?> contextProvider, params StepDescriptor[] subSteps)
            : base(new ExecutionContextDescriptor(contextProvider, false), subSteps) { }
        public TestCompositeStep(Func<object?> contextProvider, IEnumerable<StepDescriptor> subSteps)
            : base(new ExecutionContextDescriptor(contextProvider, false), subSteps) { }
        public TestCompositeStep(ExecutionContextDescriptor contextDescriptor, params StepDescriptor[] subSteps)
            : base(contextDescriptor, subSteps) { }

        public static TestCompositeStep Create(params Action[] steps)
        {
            return new TestCompositeStep(steps.Select(TestStep.CreateSync).ToArray());
        }

        public static TestCompositeStep Create(params Func<Task>[] steps)
        {
            return new TestCompositeStep(steps.Select(TestStep.Create).ToArray());
        }

        public static TestCompositeStep CreateFromComposites(params Func<TestCompositeStep>[] steps)
        {
            return new TestCompositeStep(steps.Select(TestStep.CreateComposite).ToArray());
        }
    }
}