﻿using System;
using System.Collections.Generic;
using LightBDD.Reporting.Progressive.UI.Utils;

namespace LightBDD.Reporting.Progressive.UI.Models
{
    public interface IFeatureModel : IObservableStateChange
    {
        Guid Id { get; }
        string Description { get; }
        IReadOnlyList<string> Labels { get; }
        INameInfo Name { get; }
        IReadOnlyList<IScenarioModel> Scenarios { get; }
    }
}