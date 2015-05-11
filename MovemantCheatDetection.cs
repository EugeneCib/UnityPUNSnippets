using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Class responsible for remote player movement cheat detection.
Uses known character speed attribute, and compares it with calculated speed from distance and time.
Uses average values from set of measurements
Cheat reporting happens after n seconds (1) after offense detection.
Used from CharacterNetworkSync.cs on every OnPhotonSerializeView call.
*/
public class MovemantCheatDetection 
{
	public delegate void CheatHandler();
	public event CheatHandler CheatDetected;
	const float THRESHOLD = 0.5f;
	const float CHEAT_TIME = 1f;
	private float cheatDetectionTime = 0f;
	private bool cheatTimeSet = false;
	private Vector3 previousPosition = Vector3.zero;
	private StatController statController;
	private double prevTime = 0;
	private FixedSizeQueue<MoveCheatDetectionLogItem> movemantLog;

	public MovemantCheatDetection (StatController statController) {
		this.statController = statController;
		movemantLog = new FixedSizeQueue<MoveCheatDetectionLogItem>();
		movemantLog.Limit = 20;
	}

	public void SetNext(Vector3 nextPosition) {
		if(prevTime == 0) {
			prevTime = PhotonNetwork.time;
			previousPosition = nextPosition;
			return;
		}
		double time = PhotonNetwork.time;

		if(previousPosition == nextPosition) {
			movemantLog.Clear();
		}
		else {
			float distance = Vector3.Distance(previousPosition, nextPosition); 
			float newTimeDif = (float) (time - prevTime);
			movemantLog.Enqueue(new MoveCheatDetectionLogItem { 
				deltaTime = newTimeDif,
				speed = (distance / newTimeDif), // calculates real speed
				statSpeed = statController.stats.speed
			}); 

			float totalSpeed = 0;
			float totalStatSpeed = 0;
			int count = movemantLog.list.Count;
			for(int i = 0; i < count; i++) {
				totalSpeed += movemantLog.list[i].speed;
				totalStatSpeed += movemantLog.list[i].statSpeed;
			}

			float avgSpeed = totalSpeed / count;
			float avgStatSpeed = totalStatSpeed / count;

			if(avgSpeed > (avgStatSpeed + MovemantCheatDetection.THRESHOLD) ) {
				if(cheatTimeSet == false) {
					cheatTimeSet = true;
					cheatDetectionTime = Time.time;
                }
			
				if(Time.time - cheatDetectionTime > MovemantCheatDetection.CHEAT_TIME) {
					if(CheatDetected != null) CheatDetected();
				}
			}
			else {
				cheatDetectionTime = 0;
				cheatTimeSet = false;
            }
		}
		
		prevTime = time;
		previousPosition = nextPosition;
	}
}
