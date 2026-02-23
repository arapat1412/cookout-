//FILE:GameMode
using System;

public enum GameMode
{
    Coop,
    PvP,
    PvP_3Team
}

public enum Team
{
    None = -1,
    Blue = 0,
    Red = 1,
    Yellow = 2
}
public enum PlayerRole
{
    Chef,      // Bếp trưởng
    SousChef   // Phụ bếp
}