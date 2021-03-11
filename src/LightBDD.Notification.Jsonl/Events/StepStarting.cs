﻿using System;
using System.Text.Json.Serialization;

namespace LightBDD.Notification.Jsonl.Events
{
    public sealed class StepStarting : Event
    {
        [JsonPropertyName("i")]
        public Guid Id { get; set; }
    }
}