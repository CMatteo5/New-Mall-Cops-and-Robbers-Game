using Unity.Netcode.Components;

/// <summary>
/// A NetworkTransform variant where the OWNER (not the server) is authoritative
/// over position/rotation. Use this on any object whose position should be driven
/// by whichever client currently "has" it (e.g. a carried pickup item), so that
/// client sees zero-latency movement instead of a server round-trip.
/// </summary>
public class OwnerNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative() => false;
}
