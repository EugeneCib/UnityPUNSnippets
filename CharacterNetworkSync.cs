using UnityEngine;
using System.Collections;

/**
Class responsible for character state synchronization.
Uses interpolation and extrapolation for smooth and precise position display.
Rotation calculation of character uses internal game logic of character aiming.
Movement prediction uses reuses same logic from game mechanics as for local player character movement.
*/
public class CharacterNetworkSync : Photon.MonoBehaviour 
{
	private Vector3 correctCharacterPosition = Vector3.zero;
	private GameObject characterObject;
	private MovemantController movemantController;
	private AnimationController animationController;
	private StatController statController;
	private Character character;

	private bool hasInputHorizontal = false;
	private float inputHorizontal = 0f;
	private bool hasInputVertical = false;
	private float inputVertical = 0f;
	private byte inputByte = 0;

	private MovemantCheatDetection movemantCheatDetection;
	
	void Awake()
	{
		character = GetComponent<Character>();
		characterObject = character.characterObject;
		movemantController = GetComponent<MovemantController>();
		movemantController.PositionSet += InstantPositionChange;

		animationController = GetComponent<AnimationController>();
		statController = GetComponent<StatController>();
	}

	void Start()
	{
		if(!photonView.owner.isLocal)
		{
			movemantCheatDetection = new MovemantCheatDetection(statController);
			movemantCheatDetection.CheatDetected += CheatDetected;
		}
	}

	private byte SetBitInByte(byte byteValue, byte bitPosition, bool value)
	{
		if(value)
		{
			byteValue |= (byte) (1 << bitPosition);
		}
		else
		{
			byteValue &= (byte) (~(1 << bitPosition));
		}

		return byteValue;
	}

	private bool GetBitValue(byte byteValue, int bitPosition)
	{
		return (byteValue & (1 << bitPosition)) != 0;
	}

	void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		inputByte = 0;
		if(stream.isWriting)
		{
			if(movemantController.verticalMove != 0)
			{
				inputByte = SetBitInByte(inputByte, 1, true);
				if(movemantController.verticalMove > 0) inputByte = SetBitInByte(inputByte, 2, true);
			}
			
			if(movemantController.horizontalMove != 0)
			{
				inputByte = SetBitInByte(inputByte, 5, true);
				if(movemantController.horizontalMove > 0) inputByte = SetBitInByte(inputByte, 6, true);
			}

			stream.SendNext(statController.health.value);
			stream.SendNext(statController.mana.value);
			stream.SendNext(transform.position);
			stream.SendNext(inputByte);

		}
		else
		{
			statController.health.value = (float) stream.ReceiveNext();
			statController.mana.value = (float) stream.ReceiveNext();
			correctCharacterPosition = (Vector3)stream.ReceiveNext();

			inputByte = (byte) stream.ReceiveNext();

			hasInputVertical = GetBitValue(inputByte, 1);
			if(hasInputVertical)
			{
				inputVertical = GetBitValue(inputByte, 2) == true ? 1 : -1;
			}
			else
			{
				inputVertical = 0;
			}

			hasInputHorizontal = GetBitValue(inputByte, 5);
			if(GetBitValue(inputByte, 5))
			{
				inputHorizontal = GetBitValue(inputByte, 6) == true ? 1 : -1;
			}
			else
			{
				inputHorizontal = 0;
			}

			if(movemantCheatDetection != null) movemantCheatDetection.SetNext(correctCharacterPosition);

			movemantController.SetMovemant(inputVertical, inputHorizontal, statController.stats.speed);
			StartPositionLerping();
		}

	}

	public float maxLerpTime = 0.1f;
	public float lerpTimeNeeded = 0f;
	public float lerpTime = 0f;
	public float lerpPercentage = 1f;
	public Vector3 lerpStartPosition = Vector3.zero;
	public float lerpDistance = 0f;
	public bool interpolate = false;

	public void StartPositionLerping()
	{
		lerpStartPosition = transform.position;
		lerpDistance = Vector3.Distance(lerpStartPosition, correctCharacterPosition);
		interpolate = lerpDistance > 0;
		if(interpolate)
		{
			lerpTimeNeeded = Mathf.Min(lerpDistance / statController.stats.speed, maxLerpTime);
			lerpPercentage = 0f;
			lerpTime = 0f;
		}
	}

	public float maxExtrapolationTime = 0.1f;
	public float extrapolationTime = 2f;
	public bool extrapolate = false;

	public void StartPositionExtrapolation()
	{
		extrapolate = hasInputVertical | hasInputHorizontal;
		if(extrapolate)
		{
			extrapolationTime = 0f;
		}
	}

	private float deltaTimeLeft;
	public void Update()
	{
		if (!photonView.isMine)
		{
			deltaTimeLeft = Time.deltaTime;
			if(interpolate && (lerpPercentage < 1))
			{
				Interpolate();
			}

			if(extrapolate && (extrapolationTime < maxExtrapolationTime))
			{
				Extrapolate();
			}
			else
			{
				movemantController.RotateTowardsLookPoint();
			}


			animationController.SetSpeed((interpolate || extrapolate) ? statController.stats.speed : 0);
		}
	}

	private void Interpolate()
	{
		float prevLerpTime = lerpTime;
		lerpTime += deltaTimeLeft;
		lerpPercentage = Mathf.Min (lerpTime / lerpTimeNeeded, 1);
		if(lerpPercentage == 1)
		{
			deltaTimeLeft = Mathf.Max (deltaTimeLeft - (lerpTimeNeeded - prevLerpTime), 0);
		}
		else
		{
			deltaTimeLeft = 0;
		}
		transform.position = Vector3.Lerp(transform.position, correctCharacterPosition, lerpPercentage);
		
		movemantController.UpdateMoveVector();
		movemantController.RotateTowardsLookPoint();
		
		if(lerpPercentage == 1)
		{
			interpolate = false;
			StartPositionExtrapolation();
		}
	}

	private void Extrapolate()
	{
		extrapolationTime += deltaTimeLeft;
		movemantController.UpdateMove(deltaTimeLeft);
	}


	private void InstantPositionChange(Vector3 position)
	{
		transform.position = correctCharacterPosition = position;
	}

	public void OnLeftRoom()
	{
		enabled = false;
	}

}


