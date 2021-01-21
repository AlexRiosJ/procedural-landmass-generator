using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject {

    public event System.Action OnValueUpdated;
    public bool autoUpdate;

    protected virtual void OnValidate () {
        if (autoUpdate) {
            NotifyOfUpdatedValues ();
        }
    }

    public void NotifyOfUpdatedValues () {
        if (OnValueUpdated != null) {
            OnValueUpdated ();
        }
    }

}