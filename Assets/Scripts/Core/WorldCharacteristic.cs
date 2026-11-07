using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldCharacteristic : MonoBehaviour
{
    [Header("Гравитация")]
    [Space(2.5f)]
    public float WORLD_GRAVITY;

    [Space(10f)]

    [Header("Время")]
    [Space(2.5f)]
    public float WORLD_TIMESCALE_DEFAULT = 1f;
    public float WORLD_TIMESCALE_CURRENT = 1f;
}
