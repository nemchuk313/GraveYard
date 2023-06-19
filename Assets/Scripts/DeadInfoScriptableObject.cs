using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDeadInfo", menuName = "Custom/ScriptableObject/Dead Info")]
public class DeadInfoScriptableObject : ScriptableObject
{
    public string name;
    public string surname;
    public string bornYear;
    public string deathYear;
    public string lastWords;

    public Sprite portrait;

}
