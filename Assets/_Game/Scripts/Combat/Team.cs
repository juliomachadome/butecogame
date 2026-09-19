namespace ButecoDosDevs.Combat
{
    /// <summary>
    /// Combat team. A Hitbox never hits a Hurtbox on the same team.
    /// Neutral is used by training dummies / neutral targets — hittable by the Player.
    /// </summary>
    public enum Team
    {
        Player,
        Ally,
        Enemy,
        Neutral
    }
}
