using UnityEngine;

public partial class dummy : MonoBehaviour
{
    public KMBombModule module;
    public KMSelectable button;

    private void Awake()
    {
        button.OnInteract += delegate () { module.HandlePass(); return false; };
    }

}
