using System.Collections;
using System.Collections.Generic;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Generated;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct Dualing
{
	public Ball visual;
	public Ball physics;
}

public class MyGame : BallBehavior {
	private Scene hiddenScene;
	private Scene mainScene;

	public float simulationTime = 1f;
	public GameObject root;
	public GameObject marker;

	public float hitForce;

	private string hiddenSceneName = "hidden-scene";
	private Dictionary<string, Dualing> pairs = new Dictionary<string, Dualing>();

	private List<GameObject> markers = new List<GameObject>();

	private float forceFraction = 0;
	private float directionFraction = 0.5f;

	private Vector3 force {
		get {
			return direction * forceFraction * hitForce;
		}
	}

	private Vector3 direction
	{
		get {
			return Quaternion.Euler(0, directionFraction * 360 - 180, 0) * Vector3.right;
		}
	}

	void Start () {
		Physics.autoSimulation = false;

		hiddenScene = SceneManager.CreateScene(hiddenSceneName, new CreateSceneParameters(LocalPhysicsMode.Physics3D));

		mainScene = SceneManager.GetActiveScene();

		SceneManager.SetActiveScene(hiddenScene);

		GameObject.Instantiate(root);

		FixupHiddenScene(hiddenScene);	

		SceneManager.SetActiveScene(mainScene);	
	}

	public void RegisterBall(Ball ball)
	{
		if (!pairs.ContainsKey(ball.gameObject.name))
		{
			pairs[ball.gameObject.name] = new Dualing();
		}

		var balls = pairs[ball.gameObject.name];

		if (string.Compare(ball.gameObject.scene.name, hiddenScene.name) == 0)
		{
			balls.physics = ball;
		}
		else
		{
			balls.visual = ball;
		}

		pairs[ball.gameObject.name] = balls;
	}

	void SyncInvisibleTransforms()
	{
		foreach (var pair in pairs)
		{
			var balls = pair.Value;

			// assuming duality of representations
			Ball visual = balls.visual;
			Ball hidden = balls.physics;
			var rb = hidden.GetComponent<Rigidbody>();

			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;

			hidden.transform.position = visual.transform.position;
			hidden.transform.rotation = visual.transform.rotation;
		}	
	}

	void FixupHiddenScene(Scene hiddenScene)
	{
		foreach (var sceneRoot in hiddenScene.GetRootGameObjects())
		{
			FixupHiddenGameObject(sceneRoot);
		}
	}

	void FixupHiddenGameObject(GameObject g)
	{
		var renderer = g.GetComponent<MeshRenderer>();

		if (renderer)
			Destroy(renderer);

		for (int i = 0; i < g.transform.childCount; ++i)
		{
			FixupHiddenGameObject(g.transform.GetChild(i).gameObject);
		}
	}

	public void CreateMovementMarkers()
	{
		foreach (var pair in pairs)
		{
			var balls = pair.Value;

			// assuming duality of representations
			Ball hidden = balls.physics;

			GameObject g = GameObject.Instantiate(marker, hidden.transform.position, Quaternion.identity);

			markers.Add(g);
		}	
	}

	public void CleanupMovementMarkers()
	{
		foreach (var g in markers)
		{
			Destroy(g);
		}
		markers.Clear();
	}

	void FixedUpdate()
	{
		mainScene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
	}

	private void ShowTrajectory()
	{
		SyncInvisibleTransforms();
		CleanupMovementMarkers();

		pairs["hitBall"].physics.GetComponent<Rigidbody>().AddForce(force);

		int steps = (int)(simulationTime / Time.fixedDeltaTime);
		for (int i = 0; i < steps; ++i)
		{
			// to reduce the load, we can create visual markers when a ball actuall moved some pre-set distance
			hiddenScene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
			CreateMovementMarkers();
		}
	}

	public void Hit()
	{
	networkObject.SendRpc(RPC_SHOOT,Receivers.AllBuffered,force);
	}

	public void OnDirectionChanged(float value)
	{
		directionFraction = value;
		ShowTrajectory();
	}

	public void OnForceChanged(float value)
	{
		forceFraction = value;
		ShowTrajectory();
	}

    public override void Shoot(RpcArgs args)
    {
       Vector3 Force = args.GetNext<Vector3>();
	   pairs["hitBall"].visual.GetComponent<Rigidbody>().AddForce(Force);
    }
}
