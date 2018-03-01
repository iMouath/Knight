using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseScript : MonoBehaviour
{

	public Texture2D cursorTexture2D;

//	public GameObject mousePoint;
	private CursorMode mode = CursorMode.ForceSoftware;

	private Vector2 hotspot = Vector2.zero;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		Cursor.SetCursor(cursorTexture2D, hotspot, mode);
	}
}
