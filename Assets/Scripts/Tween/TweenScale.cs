using UnityEngine;

public class TweenScale : TweenManager.Tween {

	private Vector3 initialValues;

	public void Initialize(int id, Transform targetTransform, AnimationCurve animationCurve, System.Action<TweenManager.Tween> managerCallBackFunction, System.Action callBackFunction) {
		if (animationCurve.length >= 2) {
			this.id = id;
			this.targetTransform = targetTransform;
			this.initialValues = targetTransform.localScale;
			this.animationCurve = animationCurve;
			this.beginTime = Time.time;
			this.endTime = Time.time + animationCurve.keys[animationCurve.length - 1].time;
			this.managerCallBackFunction = managerCallBackFunction;
			this.callBackFunction = callBackFunction;
		} else {
			Debug.LogWarning("The tween " + id + " does not have enough KeyFrames (" + animationCurve.length + ").");
			this.targetTransform = targetTransform;
			this.initialValues = targetTransform.localScale;
			this.endTime = 0f;
		}
	}

	void Update() {
		if (Time.time <= endTime) {
			targetTransform.localScale = initialValues * (animationCurve.Evaluate((Time.time - beginTime) / (endTime - beginTime)));
		} else {
			Stop();
		}
	}

	//Stops the tweening and returns to initialValues immediately
	public override void Stop() {
		targetTransform.localScale = initialValues;
		managerCallBackFunction(this);
		if (callBackFunction != null) {
			callBackFunction();
		}
		Destroy(this);
	}
}
