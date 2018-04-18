using UnityEngine;

public class MouseScript : MonoBehaviour {
    public Texture2D cursorTexture2D;

    private readonly Vector2 hotspot = Vector2.zero;

//	public GameObject mousePoint;
    private readonly CursorMode mode = CursorMode.ForceSoftware;

    // Use this for initialization
    private void Start() {
    }

    // Update is called once per frame
    private void Update() {
        Cursor.SetCursor(cursorTexture2D, hotspot, mode);
    }
}
