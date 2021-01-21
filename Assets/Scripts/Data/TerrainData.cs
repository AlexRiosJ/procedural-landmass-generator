using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu ()]
public class TerrainData : UpdatableData {

    public float uniformScale = 2f;
    public bool useFalloff;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public bool useFlatShading;

    public float minHieght {
        get {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate (0);
        }
    }

    public float maxHieght {
        get {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate (1);
        }
    }

}