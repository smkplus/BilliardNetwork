using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour {

	// Use this for initialization
	void Start () {
		var db = FindObjectOfType<MyGame>();
		db.RegisterBall(this);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
