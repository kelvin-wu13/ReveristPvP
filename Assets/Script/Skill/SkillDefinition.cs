using UnityEngine;

// Enum defining the basic spell types that can be input
public enum SkillType
{
    None,
    Q,
    W,
    E
}

// Enum defining all possible spell combinations
public enum SkillCombination
{
    None,
    QQ, // Q pressed twice
    QE, // Q then E
    QW, // Q then W
    WW, // W pressed twice
    WQ, // W then Q
    WE, // W then E
    EE, // E pressed twice
    EQ, // E then Q
    EW  // E then W
}