using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
	public AudioSource moveSound, kickSound, deathSound, collectKeySound;

	public static PlayerMovement instance;
	[HideInInspector] public Animator anim;
	[HideInInspector] public SpriteRenderer spriteRenderer;
	public Action onMove;

	public bool locked = false;


    public float timeToMove = 1f;
	public LayerMask collisionLayer, victoryLayer, spikeLayer, keyLayer;

	private string state = "idle";

	private Vector3 moveEndPos;
	private Vector3 moveStartPos;
	private float moveLerpTime;

	private float pushTimer;

	private void Awake()
	{
		if (instance != null && instance != this) { Destroy(this); }
		instance = this;
		anim = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	private void Update()
	{
		if (locked) return;
		switch (state) {
			case "idle":
				if (Manager.instance.moves <= 0) return;

				float horizontalInput = Input.GetAxisRaw("Horizontal");
				float verticalInput = Input.GetAxisRaw("Vertical");

				// return if no movement
				if (horizontalInput == 0 && verticalInput == 0) return;

				moveEndPos = transform.position;

				if (horizontalInput != 0) moveEndPos += new Vector3(horizontalInput, 0, 0);
				else if (verticalInput != 0) moveEndPos += new Vector3(0, verticalInput, 0);

				Collider2D collision = Physics2D.OverlapCircle(moveEndPos, 0.2f, collisionLayer);
				// if collision check for "special" collisions
				if (collision)
				{
					if (collision.TryGetComponent(out PushBlock pushBlock))
					{
						// pushblock.push returns a bool based on if block can be pushed
						if (!pushBlock.Push(moveEndPos - transform.position)) ChangeState("pushing");
						else return;
					}
					else if (collision.CompareTag("Door") && Manager.instance.keys > 0)
                    {
						collectKeySound.Play();
						Manager.instance.keys--;
						Destroy(collision.gameObject);
                        ChangeState("moving");
                    }
					// return if collision
					else return;
				}
				else { 
					ChangeState("moving");
				}
				// if succesfully gotten here move must have been sucessful
				onMove?.Invoke();

				break;
			case "moving":
				moveLerpTime += Time.deltaTime;

				float t = moveLerpTime / timeToMove;
				// ease out function
				t = Mathf.Sin(t * Mathf.PI * 0.5f);
				transform.position = Vector3.Lerp(moveStartPos, moveEndPos, t);

				if (Vector3.Distance(transform.position, moveEndPos) < 0.05f)
				{
					transform.position = moveEndPos;
					ChangeState("idle");
				}
			break;
			case "pushing":
				pushTimer -= Time.deltaTime;
				if (pushTimer < 0) ChangeState("idle");
			break;
		}
	}

	public void ChangeState(string newState)
	{
		state = newState;

		if (state == "idle")
		{
			anim.Play("Idle");
			Collider2D[] collisionCollider = Physics2D.OverlapCircleAll(transform.position, 0.2f);
			bool checkMoves = true;
			if (locked) return;
			if (collisionCollider != null) {
				foreach (Collider2D collision in collisionCollider)
				{
					if (CheckLayer(collision.gameObject, victoryLayer))
					{
						Manager.instance.Continue();
						ChangeState("win");
						return;
					}
					else if (CheckLayer(collision.gameObject, keyLayer))
					{
						collectKeySound.Play();
						Manager.instance.keys++;
						Destroy(collision.gameObject);
					}
					else if (CheckLayer(collision.gameObject, spikeLayer))
					{
						Die();
						checkMoves = false;
					}
				}
			}
            if (checkMoves) Manager.instance.CheckMoves();
        }

		if (state == "moving")
		{
			moveSound.Play();
			anim.Play("Move");
			if (moveEndPos.y - transform.position.y == 0) spriteRenderer.flipX = (moveEndPos.x - transform.position.x) < 0;
			UpdateMove();
			moveLerpTime = 0;
			moveStartPos = transform.position;
		}

		if (state == "pushing")
		{
			kickSound.Play();
			anim.Play("Kick");
			if (moveEndPos.y - transform.position.y == 0) spriteRenderer.flipX = (moveEndPos.x - transform.position.x) < 0;
			UpdateMove();
			pushTimer = 0.2f;
		}

		if (state == "die")
		{
			deathSound.Play();
			anim.Play("Death");
		}
	}

	void UpdateMove()
	{
		Manager.instance.Move();
	}

	public void Die()
	{
		Manager.instance.Die();
		ChangeState("die");
	}

	bool CheckLayer(GameObject obj, LayerMask layer)
    {
		return (layer | (1 << obj.layer)) == layer;
    }
}
