using UnityEngine;

public static class Constants
{
    // Base movement speed in world units per second.
    // Lowered a bit to make motion feel less "teleporty" on screen.
    public static float speed = 3.0f;

    // Alignment strings
    public const string ALIGNMENT_NEUTRAL   = "Neutral";
    public const string ALIGNMENT_FRIENDLY  = "Friendly";
    public const string ALIGNMENT_PREY      = "Prey";
    public const string ALIGNMENT_PREDATOR  = "Predator";
    public const string ALIGNMENT_HOSTILE   = "Hostile"; // survivors only

    // Agent types
    public const string AGENT_TYPE_KILLER   = "Killer";
    public const string AGENT_TYPE_SURVIVOR = "Survivor";
    public const string AGENT_TYPE_HERO     = "Hero";

    // Observation sizes (must match CollectObservations exactly)
    public const int KILLER_OBS_SIZE   = 12; // 3+3+1+1+4
    public const int SURVIVOR_OBS_SIZE = 12; // 4+4+1+1+2
}