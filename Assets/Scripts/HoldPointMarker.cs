using UnityEngine;

/// <summary>
/// Add this to your player's HoldPoint object (wherever it is in the hierarchy -
/// under the camera, under the body, etc). Lets scripts reliably find it via
/// GetComponentInChildren, regardless of nesting depth, instead of a fragile
/// Transform.Find("HoldPoint") which only checks direct children.
/// </summary>
public class HoldPointMarker : MonoBehaviour { }
