// Copyright (C) Neurosoft

using System.Collections.Generic;
using SpeechMarkupEditor.Models;

namespace SpeechMarkupEditor.Messages;

public class AddWordResult
{
    public bool Added { get; set; }

    public List<WordTimestamp> OverlappingWords { get; set; } = [];
}