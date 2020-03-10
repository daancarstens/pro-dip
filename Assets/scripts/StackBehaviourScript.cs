﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StackBehaviourScript : MonoBehaviour {

	private const string START_MENU = "startmenu";
	private const string PLAYING = "playing";
	private const string GAME_OVER = "gameover";

	private const string HIGH_SCORE = "highscore";
		
	private const float BLOCK_WIDTH = 1f; 
	private const float BLOCK_HEIGHT = 0.25f;
	private const float BLOCK_BOUNDS = 1.5f; // how far blocks slide backwards and forwards
	private const float TOLERANCE = 0.05f; // size you can miss by and still get a perfect block
	private const int PERFECT_BONUS = 8; // number of perfect blocks to get a growth bonus
	private const float PERFECT_INCREASE = 0.1f; // size block grows
	private const int TOTAL_COLOURS = 7;
	private const float TILE_START_SPEED = 1.5f;
	private const float TILE_SPEED_INCREASE = 0.01f;
	private float tileSpeed =0.0f;
	private float tileTransition = 0.0f;

	private string state;
	private int blockCount =1;

	// colours
	private Color[] blockColours;
	private Color currentColour;
	private int currentColourIndex;

	// GameObjects
	private GameObject topBlock;
	private GameObject movingBlock;
	private bool movingBlockXDir;
	public Camera theCamera;
	private Vector3 cameraStartPoint;
	private Quaternion cameraStartRotation;
	private GameObject perfectHalo;
	private Color perfectHaloColor;
	public Material transparentMaterial;

	public GameObject baseBlock; 
	public UnityEngine.UI.Text scoreLabel;
	public UnityEngine.UI.Text hiScoreLabel;
	public UnityEngine.UI.Button playButton;
	public UnityEngine.UI.Text playButtonLabel;

	// audio
	public AudioClip[] notes;
	public AudioSource audioSource;

	private int score;
	private int perfectCount;



	private bool blockAdded;

	void Start () {
	
		// reset game settings
		InitGame();

	}

	// Called once
	void InitGame() {
		state = START_MENU;

		Screen.orientation = ScreenOrientation.Portrait;
		Screen.SetResolution (480, 800, false);

		playButtonLabel.text = "Play";
		playButton.gameObject.SetActive (true);
		hiScoreLabel.gameObject.SetActive (true);

		baseBlock.GetComponent<Renderer>().material.color = new Color(1,0,0);

		updateHiScoreLabel ();

		// init colours to use
		blockColours = new Color[TOTAL_COLOURS];
		blockColours [0] = new Color (1f, 0f, 0f);
		blockColours [1] = new Color (0f, 1f, 0f);
		blockColours [2] = new Color (0f, 0f, 1f);
		blockColours [3] = new Color (1f, 1f, 0f);
		blockColours [4] = new Color (1f, 0f, 1f);
		blockColours [5] = new Color (0f, 1f, 1f);
		blockColours [6] = new Color (1f, 1f, 1f);
	}
		
	// Called at every Start of game
	public void StartGame() {
		score = 0;
		blockCount = 0;
		perfectCount = 0;
		tileSpeed = TILE_START_SPEED;

		// reset camera pos
		cameraStartPoint = new Vector3(-1f,3f,-1f);
		cameraStartRotation =  Quaternion.Euler(45f, 45f, 0f);
		theCamera.orthographicSize = 1.5f;

		theCamera.transform.SetPositionAndRotation (cameraStartPoint, cameraStartRotation);


		// destroy all previous child blocks
		foreach (Transform child in baseBlock.transform) {
			DestroyObject (child.gameObject);
		}

		// default topBlock to cube in stack
		topBlock = NewTopBlock (BLOCK_WIDTH, BLOCK_HEIGHT, BLOCK_WIDTH, 0, BLOCK_WIDTH+BLOCK_HEIGHT/2, 0);
		topBlock.GetComponent<Renderer> ().material = baseBlock.GetComponent<Renderer> ().material;

		currentColourIndex = TOTAL_COLOURS-1;
		NextColour ();

		// set Colour of first block
		topBlock.GetComponent<Renderer>().material.color = currentColour;		
		movingBlockXDir = true;

		if (movingBlock != null) {
			DestroyObject(movingBlock);
		}
		// init moving block
		NewMovingBlock();

		state = PLAYING;
		playButton.gameObject.SetActive (false);
		hiScoreLabel.gameObject.SetActive (false);

	}

	private void updateHiScoreLabel() {
		int highScore = PlayerPrefs.GetInt(HIGH_SCORE);
		hiScoreLabel.text = "Hi:" + highScore.ToString ();
	}

	private void gameOver() {
		state = GAME_OVER;
		// drop moving block
		Rigidbody rigidBody = movingBlock.AddComponent<Rigidbody>();
		rigidBody.mass = 5;

		// save high score
		int highScore = PlayerPrefs.GetInt(HIGH_SCORE);

		if (score > highScore) {
			// update highscore
			PlayerPrefs.SetInt(HIGH_SCORE,score);
			updateHiScoreLabel ();
		}

		playButtonLabel.text = "Again?";
		playButton.gameObject.SetActive (true);
		hiScoreLabel.gameObject.SetActive (true);
	}

	// Update is called once per frame
	void Update () {

		switch (state) {
		case START_MENU:
			{
				break;
			}
		case PLAYING:
			{
				playingUpdate ();
				break;
			}
		case GAME_OVER:
			{
				gameOverUpdate ();
				break;
			}
		}

	}

	// called every update in GAME_OVER state
	void gameOverUpdate() {
		// zoom outwards
		if (theCamera.orthographicSize < 10) {
			theCamera.orthographicSize += 0.5f * Time.deltaTime;
		}

		// rotate around stack
		theCamera.transform.RotateAround(Vector3.zero, Vector3.up, 25 * Time.deltaTime);
	}

	// called every update in PLAYING state
	void playingUpdate() {

		if (Input.anyKeyDown) {
			blockAdded = AddCube ();
			if (!blockAdded) {
				gameOver ();
			} else {
				blockCount++;
				score++;
				scoreLabel.text = score.ToString ();
				// speed up
				tileSpeed += TILE_SPEED_INCREASE;
			}
			tileTransition = 0;
		} else {
			tileTransition += Time.deltaTime * tileSpeed;
			if (movingBlockXDir) {
				// slide block on x axis
				movingBlock.transform.position = new Vector3 (Mathf.Sin (tileTransition) * BLOCK_BOUNDS, movingBlock.transform.position.y, movingBlock.transform.position.z);
			} else {
				// slide block on z axis
				movingBlock.transform.position = new Vector3 (movingBlock.transform.position.x,movingBlock.transform.position.y,Mathf.Sin (tileTransition) * BLOCK_BOUNDS);
			} 
		}

		updateAnimations ();

	}

	void updateAnimations() {

		if(perfectHalo != null) {
			// decrease alpha value
			perfectHaloColor.a -= 1f * Time.deltaTime;
			perfectHalo.GetComponent<Renderer> ().material.color = perfectHaloColor;

			if (perfectHaloColor.a < 0.1f) {
				Destroy (perfectHalo);
			} else {

			}

		}
	
	}

	bool AddCube() {

		// Drop moving block onto top of stack

		// calc dimensions of block to add based on size of the overlap between moving block and top block

		Vector3 tPos = topBlock.transform.position;
		Vector3 tScale = topBlock.transform.localScale;

		Vector3 mPos = movingBlock.transform.position;// + movingBlock.transform.localPosition;
		Vector3 mScale = movingBlock.transform.localScale;

		// calc min and max x,z co-ords for both cubes
		float tMinX = tPos.x - tScale.x/2;
		float tMaxX = tPos.x + tScale.x/2;
		float tMinZ = tPos.z - tScale.z/2;
		float tMaxZ = tPos.z + tScale.z/2;

		float mMinX = mPos.x - mScale.x/2;
		float mMaxX = mPos.x + mScale.x/2;
		float mMinZ = mPos.z - mScale.z/2;
		float mMaxZ = mPos.z + mScale.z/2;

		// use the LARGEST min and the SMALLEST max values
		// for the resulting cube

		float rMinX, rMaxX, rMinZ, rMaxZ;

		if (tMinX >= mMinX) {
			rMinX = tMinX;
		} else {
			rMinX = mMinX;
		}
		if (tMaxX >= mMaxX) {
			rMaxX = mMaxX;
		} else {
			rMaxX = tMaxX;
		}

		if (tMinZ >= mMinZ) {
			rMinZ = tMinZ;
		} else {
			rMinZ = mMinZ;
		}
		if (tMaxZ >= mMaxZ) {
			rMaxZ = mMaxZ;
		} else {
			rMaxZ = tMaxZ;
		}

		// dimensions of current top block
		float txWidth = tScale.x;
		float tzWidth = tScale.z;

		// scale movingBlock
		float xWidth = rMaxX-rMinX;
		float zWidth = rMaxZ-rMinZ;

		float xDiff = Mathf.Abs(txWidth-xWidth);
		float zDiff = Mathf.Abs(tzWidth-zWidth);

		bool perfectDrop = false;
		// if new width is within tolerance, stay same size
		if (xDiff <= TOLERANCE && zDiff <= TOLERANCE) {
			perfectDrop = true;
			// resize
			xWidth = txWidth;
			zWidth = tzWidth;
			perfectCount++;
			PlayNextNote ();
			NewPerfectHalo ();
		} else {
			perfectCount = 0;
		}


		if (xWidth < 0 || zWidth < 0) {
			// Game over
			return false;
		}
			
		// centre new block, based on intersection of movingBlock and topBlock
		float centreX = movingBlock.transform.position.x;
		float centreZ = movingBlock.transform.position.z; 

		// calc size/pos of broken slice
		float bxWidth = mScale.x; 
		float bzWidth = mScale.z; 

		if (!perfectDrop) {
			// add broken block if this is not a perfect drop	
			if (movingBlockXDir) {
				if (mMaxX >= tMaxX) {
					// moving block is further than top block
					centreX = (tMaxX - xWidth / 2);

					bxWidth = mMaxX - tMaxX;
					float bxPos = centreX + xWidth / 2 + bxWidth / 2;
					NewBrokenBlock (bxPos, mPos.y, mPos.z, bxWidth, BLOCK_HEIGHT, bzWidth);
				} else {
					// moving block is nearer than top block
					centreX = (mMaxX - xWidth / 2);

					bxWidth = tMinX - mMinX;
					float bxPos = centreX - xWidth / 2 - bxWidth / 2;
					NewBrokenBlock (bxPos, mPos.y, mPos.z, bxWidth, BLOCK_HEIGHT, bzWidth);
				}
			} else {
				if (mMaxZ >= tMaxZ) {
					// moving block is further than top block
					centreZ = (tMaxZ - zWidth / 2);

					bzWidth = mMaxZ - tMaxZ;
					float bzPos = centreZ + zWidth / 2 + bzWidth / 2;
					NewBrokenBlock (mPos.x, mPos.y, bzPos, bxWidth, BLOCK_HEIGHT, bzWidth);
				} else {
					// moving block is nearer than top block
					centreZ = (mMaxZ - zWidth / 2);

					bzWidth = tMinZ - mMinZ;
					float bzPos = centreZ - zWidth / 2 - bzWidth / 2;
					NewBrokenBlock (mPos.z, mPos.y, bzPos, bxWidth, BLOCK_HEIGHT, bzWidth);

				}
			}
			// update top block details
			topBlock = NewTopBlock (xWidth, BLOCK_HEIGHT, zWidth, centreX, movingBlock.transform.position.y, centreZ);
		} else {
			// this was a perfect drop so no slicing

			// if we have enough perfect drops, grow block
			if (perfectCount >= PERFECT_BONUS) {
				if(movingBlockXDir) {
					xWidth += PERFECT_INCREASE;
				} else {
					zWidth += PERFECT_INCREASE;
				}
				// reset count
				perfectCount = 0;
				if(xWidth>BLOCK_WIDTH) {
					xWidth=BLOCK_WIDTH;
				}
				if(zWidth>BLOCK_WIDTH) {
					zWidth=BLOCK_WIDTH;
				}
			}
			topBlock = NewTopBlock (xWidth, BLOCK_HEIGHT, zWidth, tPos.x, movingBlock.transform.position.y, tPos.z);
		}

		// update colour
		Color mColor = movingBlock.GetComponent<Renderer>().material.color;		
		topBlock.GetComponent<Renderer> ().material.color = mColor;		



		// destroy previous movingBlock
		Destroy(movingBlock);

		// move camera
		theCamera.transform.Translate(Vector3.up * BLOCK_HEIGHT, Space.World);

		// add a new moving block
		NewMovingBlock();
		return true;
	}

	GameObject NewTopBlock(float xWidth,float yWidth, float zWidth, float xPos, float yPos, float zPos) {
		GameObject newBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
		newBlock.name = "TopBlock:" + blockCount;
		newBlock.transform.localScale = new Vector3(xWidth, yWidth, zWidth);
		newBlock.transform.position = new Vector3 (xPos, yPos, zPos);
		// add to BaseBlock
		newBlock.transform.SetParent(baseBlock.transform);
		return newBlock;
	}

	// create a new moving block
	void NewMovingBlock() {

		// create a new cube
		movingBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
		movingBlock.name = "MovingBlock";
		// scale it
		movingBlock.transform.localScale = topBlock.transform.localScale;
		movingBlock.transform.localScale = new Vector3(movingBlock.transform.localScale.x,BLOCK_HEIGHT,movingBlock.transform.localScale.z);
		// add above top block
		movingBlock.transform.position = new Vector3(topBlock.transform.position.x, topBlock.transform.position.y+BLOCK_HEIGHT, topBlock.transform.position.z);
		movingBlock.GetComponent<Renderer> ().material = baseBlock.GetComponent<Renderer> ().material;
		//Color tColor = topBlock.GetComponent<Renderer>().material.color;

		FadeColour ();

		movingBlock.GetComponent<Renderer> ().material.color = currentColour;		

		// flip dir
		movingBlockXDir = !movingBlockXDir;

	}

	void FadeColour() {
		// decrease colour attributes
		if (currentColour.r > 0) {
			currentColour.r -= 0.1f;
			if (currentColour.r < 0.5f) {
				NextColour ();
			}
		}
		if (currentColour.g > 0) {
			currentColour.g -= 0.1f;
			if (currentColour.g < 0.5f) {
				NextColour ();
			}
		}
		if (currentColour.b > 0) {
			currentColour.b -= 0.1f;
			if (currentColour.b < 0.5f) {
				NextColour ();
			}
		}
	}

	void NextColour() {
		// increment current colour
		currentColourIndex++;
		if (currentColourIndex >= TOTAL_COLOURS - 1) {
			currentColourIndex = 0;
		}

		// create a copy of colour so it can be modified
		currentColour = new Color(blockColours [currentColourIndex].r,blockColours [currentColourIndex].g,blockColours [currentColourIndex].b);

	}

	// create a new broken block
	void NewBrokenBlock(float xPos, float yPos, float zPos, float xScale, float yScale, float zScale) {

		// create a new cube
		GameObject brokenBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
		brokenBlock.name = "BrokenBlock";
		// scale it
		brokenBlock.transform.localScale  = new Vector3(xScale,yScale,zScale);
		brokenBlock.transform.position = new Vector3(xPos,yPos,zPos);
		brokenBlock.GetComponent<Renderer> ().material.color = currentColour;

		// add physics
		Rigidbody rigidBody = brokenBlock.AddComponent<Rigidbody>();
		rigidBody.mass = 5;

		brokenBlock.transform.SetParent(baseBlock.transform);
	}

	// create a plane beneath the new block added
	void NewPerfectHalo() {

		// create a new Quad
		perfectHalo = GameObject.CreatePrimitive(PrimitiveType.Quad);
		perfectHalo.name = "PerfectHalo";
		// scale it
		perfectHalo.transform.localScale = new Vector3(topBlock.transform.localScale.x *1.1f,topBlock.transform.localScale.z *1.1f,1f);
		// add above top block
		perfectHalo.transform.position = new Vector3(topBlock.transform.position.x, topBlock.transform.position.y+BLOCK_HEIGHT/2, topBlock.transform.position.z);
		perfectHalo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		perfectHaloColor = new Color (1, 1, 1,0.5f);
		perfectHalo.GetComponent<Renderer> ().material = transparentMaterial;
		perfectHalo.GetComponent<Renderer> ().material.color = perfectHaloColor;
	}

	void PlayNextNote() {
		audioSource.PlayOneShot (notes [perfectCount - 1]);
	}
}