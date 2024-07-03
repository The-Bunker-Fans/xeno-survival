﻿using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Announce;

[Serializable, NetSerializable]
public enum MarineCommunicationsComputerUI
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MarineCommunicationsComputerMsg(string text) : BoundUserInterfaceMessage
{
    public readonly string Text = text;
}
